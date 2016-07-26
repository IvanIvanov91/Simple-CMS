using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Helpers;
using System.Web.SessionState;
using WebMatrix.Data;

public class RoleHandler : IHttpHandler, IReadOnlySessionState
{
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

        if (!WebUser.HasRole(UserRoles.Admin))
        {
            throw new HttpException(401, "Нямате достъп до тази част на системата.");
        }

        var mode = context.Request.Form["mode"];
        var name = context.Request.Form["roleName"];
        var id = context.Request.Form["roleId"];
        var resourceItem = context.Request.Form["resourceItem"];


        if (mode == "edit")
        {
            Edit(Convert.ToInt32(id), name);
        }
        else if (mode == "new")
        {
            Create(name);
        }
        else if (mode == "delete")
        {
            Delete(name ?? resourceItem);
        }

        if (string.IsNullOrEmpty(resourceItem))
        {
            context.Response.Redirect("~/admin/role/");
        }

    }

    private static void Create(string name)
    {
        var result = RoleRepository.Get(name);

        if (result != null)
        {
            throw new HttpException(409, "Вече има такава роля.");
        }

        RoleRepository.Add(name);
    }

    private static void Edit(int id, string name)
    {
        var result = RoleRepository.Get(id);

        if (result == null)
        {
            throw new HttpException(404, "Не съществува такава роля.");
        }

        RoleRepository.Edit(id, name);
    }

    private static void Delete(string name)
    {
        RoleRepository.Remove(name);
    }
}