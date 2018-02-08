/*
 * Copyright (c) 2018 Håkan Edling
 *
 * This software may be modified and distributed under the terms
 * of the MIT license.  See the LICENSE file for details.
 * 
 * http://github.com/piranhacms/piranha
 * 
 */

using Piranha.AttributeBuilder;
using Piranha.Extend.Fields;
using System;
using System.Data.SqlClient;
using System.Linq;
using Xunit;

namespace Piranha.Tests.Routers
{
    [Collection("Integration tests")]
    public class Pages : BaseTests
    {
        private Guid SITE1_ID = Guid.NewGuid();
        private Guid SITE2_ID = Guid.NewGuid();
        private Guid PAGE1_ID = Guid.NewGuid();
        private Guid PAGE2_ID = Guid.NewGuid();

        [PageType(Title = "My PageType")]
        public class MyPage : Models.Page<MyPage>
        {
            [Region]
            public TextField Body { get; set; }
        }
        

        protected override void Init() {
            using (var api = new Api(GetDb(), storage)) {
                Piranha.App.Init(api);

                var builder = new PageTypeBuilder(api)
                    .AddType(typeof(MyPage));
                builder.Build();

                // Add site
                var site1 = new Data.Site() {
                    Id = SITE1_ID,
                    Title = "Page Site",
                    InternalId = "PageSite",
                    IsDefault = true
                };
                api.Sites.Save(site1);

                var site2 = new Data.Site() {
                    Id = SITE2_ID,
                    Title = "Page Site 2",
                    InternalId = "PageSite2",
                    Hostnames = "www.myothersite.com",
                    IsDefault = false
                };
                api.Sites.Save(site2);

                // Add pages
                var page1 = MyPage.Create(api);
                page1.Id = PAGE1_ID;
                page1.SiteId = SITE1_ID;
                page1.Title = "My first page";
                page1.Body = "My first body";
                page1.Published = DateTime.Now;
                api.Pages.Save(page1);

                var page2 = MyPage.Create(api);
                page2.Id = PAGE2_ID;
                page2.SiteId = SITE2_ID;
                page2.Title = "My second page";
                page2.Body = "My second body";
                page2.Published = DateTime.Now;
                api.Pages.Save(page2);
            }
        }

        protected override void Cleanup() {
            using (var api = new Api(GetDb(), storage)) {
                var pages = api.Pages.GetAll();
                foreach (var p in pages)
                    api.Pages.Delete(p);

                var types = api.PageTypes.GetAll();
                foreach (var t in types)
                    api.PageTypes.Delete(t);

                var sites = api.Sites.GetAll();
                foreach (var s in sites)
                    api.Sites.Delete(s);
            }
        }

        [Fact]
        public void GetPageByUrlDefaultSite() {
            using (var api = new Api(GetDb(), storage)) {
                var response = Piranha.Web.PageRouter.Invoke(api, "/my-first-page", null);

                Assert.NotNull(response);
                Assert.Equal("/page", response.Route);
                Assert.Equal(true, response.IsPublished);
                Assert.Equal($"id={PAGE1_ID}&startpage=true&piranha_handled=true", response.QueryString);
            }
        }

        [Fact]
        public void GetStarpageDefaultSite() {
            using (var api = new Api(GetDb(), storage)) {
                var response = Piranha.Web.StartPageRouter.Invoke(api, "/", null);

                Assert.NotNull(response);
                Assert.Equal("/page", response.Route);
                Assert.Equal(true, response.IsPublished);
                Assert.Equal($"id={PAGE1_ID}&startpage=true&piranha_handled=true", response.QueryString);
            }
        }

        [Fact]
        public void GetPageByUrlNoneDefaultSite() {
            using (var api = new Api(GetDb(), storage)) {
                var response = Piranha.Web.PageRouter.Invoke(api, "/my-second-page", null);

                Assert.Null(response);
            }
        }

        [Fact]
        public void GetPageByUrlOtherSite() {
            using (var api = new Api(GetDb(), storage)) {
                var response = Piranha.Web.PageRouter.Invoke(api, "/my-second-page", "www.myothersite.com");

                Assert.NotNull(response);
                Assert.Equal("/page", response.Route);
                Assert.Equal(true, response.IsPublished);
                Assert.Equal($"id={PAGE2_ID}&startpage=true&piranha_handled=true", response.QueryString);
            }
        }

        [Fact]
        public void GetStartpageOtherSite() {
            using (var api = new Api(GetDb(), storage)) {
                var response = Piranha.Web.StartPageRouter.Invoke(api, "/", "www.myothersite.com");

                Assert.NotNull(response);
                Assert.Equal("/page", response.Route);
                Assert.Equal(true, response.IsPublished);
                Assert.Equal($"id={PAGE2_ID}&startpage=true&piranha_handled=true", response.QueryString);
            }
        }

        [Fact]
        public void GetPageByUrlNoneOtherSite() {
            using (var api = new Api(GetDb(), storage)) {
                var response = Piranha.Web.PageRouter.Invoke(api, "/my-first-page", "www.myothersite.com");

                Assert.Null(response);
            }
        }
    }
}
