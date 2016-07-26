using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Helpers;
using System.Web.SessionState;
using WebMatrix.Data;

public class PostHandler : IHttpHandler, IReadOnlySessionState
{
    public PostHandler()
    {

    }

    public bool IsReusable
    {
        get { return false; }
    }

    public void ProcessRequest(HttpContext context)
    {
        AntiForgery.Validate();

        if (!WebUser.IsAuthenticated)
        {
            throw new HttpException(401, "Трябва да влезете в системата.");
        }

        if (!WebUser.HasRole(UserRoles.Admin) &&
            !WebUser.HasRole(UserRoles.Editor) &&
            !WebUser.HasRole(UserRoles.Author))
        {
            throw new HttpException(401, "Нямате достъп до тази част на системата.");
        }

        var mode = context.Request.Form["mode"];
        var title = context.Request.Form["postTitle"];
        var content = context.Request.Form["postContent"];
        var slug = context.Request.Form["postSlug"];
        var id = context.Request.Form["postId"];
        var datePublished = context.Request.Form["postDatePublished"];
        var postTags = context.Request.Form["postTags"];
        var authorId = context.Request.Form["postAuthorId"];
        var resourceItem = context.Request.Form["resourceItem"];

        IEnumerable<int> tags = new int[] { };

        if (!string.IsNullOrEmpty(postTags))
        {
            tags = postTags.Split(',').Select(v => Convert.ToInt32(v));
        }

        if ((mode == "edit" || mode == "delete") && WebUser.HasRole(UserRoles.Author))
        {
            if (WebUser.UserId != Convert.ToInt32(authorId))
            {
                throw new HttpException(401, "Нямате достъп до тази част на системата.");
            }
        }

        if (mode == "delete")
        {
            DeletePost(slug ?? resourceItem);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                slug = CreateSlug(title);
            }

            if (mode == "edit")
            {
                EditPost(Convert.ToInt32(id), title, content, slug, datePublished, Convert.ToInt32(authorId), tags);
            }
            else if (mode == "new")
            {
                CreatePost(title, content, slug, datePublished, WebUser.UserId, tags);
            }
        }

        if (string.IsNullOrEmpty(resourceItem))
        {
            context.Response.Redirect("~/admin/post/");
        }

    }

    private static void CreatePost(string title, string content,
        string slug, string datePublished, int authorId, IEnumerable<int> tags)
    {
        var result = PostRepository.Get(slug);
        DateTime? published = null;

        if (result != null)
        {
            throw new HttpException(409, "Вече има такова заглавие.");
        }

        if (!string.IsNullOrWhiteSpace(datePublished))
        {
            published = DateTime.Parse(datePublished);
        }

        PostRepository.Add(title, content, slug, published, authorId, tags);
    }

    private static void EditPost(int id, string title, string content,
        string slug, string datePublished, int authorId, IEnumerable<int> tags)
    {
        var result = PostRepository.Get(id);
        DateTime? published = null;

        if (result == null)
        {
            throw new HttpException(404, "Публикацията не съшествува.");
        }

        if (!string.IsNullOrWhiteSpace(datePublished))
        {
            published = DateTime.Parse(datePublished);
        }

        PostRepository.Edit(id, title, content, slug, published, authorId, tags);
    }

    private static void DeletePost(string slug)
    {
        PostRepository.Remove(slug);
    }

    private static string CreateSlug(string title)
    {
        title = title.ToLowerInvariant().Replace(" ", "-");
        title = Regex.Replace(title, @"[^0-9a-z-]", string.Empty);

        return title;
    }
}