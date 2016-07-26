using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Helpers;
using System.Web.SessionState;
using WebMatrix.Data;

public class TagHandler : IHttpHandler, IReadOnlySessionState
{
    public TagHandler()
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
            !WebUser.HasRole(UserRoles.Editor))
        {
            throw new HttpException(401, "Нямате достъп до тази част на системата.");
        }

        var mode = context.Request.Form["mode"];
        var name = context.Request.Form["tagName"];
        var friendlyName = context.Request.Form["tagFriendlyName"];
        var id = context.Request.Form["tagId"];
        var resourceItem = context.Request.Form["resourceItem"];

        if (mode == "delete")
        {
            DeleteTag(friendlyName ?? resourceItem);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(friendlyName))
            {
                friendlyName = CreateTag(name);
            }

            if (mode == "edit")
            {
                EditTag(Convert.ToInt32(id), name, friendlyName);
            }
            else if (mode == "new")
            {
                CreateTag(name, friendlyName);
            }
        }

        if (string.IsNullOrEmpty(resourceItem))
        {
            context.Response.Redirect("~/admin/tag/");
        }

    }

    private static void CreateTag(string name, string friendlyName)
    {
        var result = TagRepository.Get(friendlyName);

        if (result != null)
        {
            throw new HttpException(409, "Вече има такъв таг.");
        }

        TagRepository.Add(name, friendlyName);
    }

    private static void EditTag(int id, string name, string friendlyName)
    {
        var result = TagRepository.Get(id);

        if (result == null)
        {
            throw new HttpException(404, "Не съществува такъв таг.");
        }

        TagRepository.Edit(id, name, friendlyName);
    }

    private static void DeleteTag(string friendlyName)
    {
        TagRepository.Remove(friendlyName);
    }

    private static string CreateTag(string name)
    {
        name = name.ToLowerInvariant().Replace(" ", "-");
        name = Regex.Replace(name, @"[^0-9a-z-]", string.Empty);

        return name;
    }
}