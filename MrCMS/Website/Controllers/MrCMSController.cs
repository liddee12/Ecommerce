﻿using System.Linq;
using System.Web;
using System.Web.Mvc;
using MrCMS.Apps;
using MrCMS.Entities;
using MrCMS.Entities.Multisite;

namespace MrCMS.Website.Controllers
{
    public abstract class MrCMSAppUIController<T> : MrCMSUIController where T : MrCMSApp, new()
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            RouteData.DataTokens["app"] = new T().AppName;
            base.OnActionExecuting(filterContext);
        }
    }

    public abstract class MrCMSController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            CheckCurrentSite(filterContext);
            base.OnActionExecuting(filterContext);
        }

        private void CheckCurrentSite(ActionExecutingContext filterContext)
        {
            var entities = filterContext.ActionParameters.Values.OfType<SiteEntity>();

            if (entities.Any(entity => entity.Site != CurrentSite && entity.Id != 0))
            {
                filterContext.Result = AuthenticationFailureRedirect();
            }
        }
        public string ReferrerOverride { get; set; }
        protected string Referrer
        {
            get { return ReferrerOverride ?? HttpContext.Request.UrlReferrer.ToString(); }
        }

        private Site _currentSite;

        public Site CurrentSite
        {
            get { return _currentSite ?? CurrentRequestData.CurrentSite; }
            set { _currentSite = value; }
        }

        public new HttpRequestBase Request
        {
            get { return RequestMock ?? base.Request; }
        }

        public HttpRequestBase RequestMock { get; set; }

        protected virtual RedirectResult AuthenticationFailureRedirect()
        {
            return Redirect("~");
        }
    }
}