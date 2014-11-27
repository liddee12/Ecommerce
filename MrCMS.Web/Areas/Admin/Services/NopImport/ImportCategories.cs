using System;
using System.Collections.Generic;
using System.Linq;
using MrCMS.Entities.Documents.Web;
using MrCMS.Helpers;
using MrCMS.Models;
using MrCMS.Services;
using MrCMS.Web.Apps.Ecommerce.Pages;
using MrCMS.Web.Areas.Admin.Services.NopImport.Models;
using MrCMS.Website;
using NHibernate;

namespace MrCMS.Web.Areas.Admin.Services.NopImport
{
    public class ImportCategories : IImportCategories
    {
        private readonly IUniquePageService _uniquePageService;
        private readonly ISession _session;
        private readonly IWebpageUrlService _webpageUrlService;

        public ImportCategories(IUniquePageService uniquePageService, ISession session, IWebpageUrlService webpageUrlService)
        {
            _uniquePageService = uniquePageService;
            _session = session;
            _webpageUrlService = webpageUrlService;
        }

        public string ProcessCategories(INopCommerceProductReader nopCommerceProductReader, string connectionString,
            NopImportContext nopImportContext)
        {
            List<CategoryData> categoryDatas = nopCommerceProductReader.GetCategoryData(connectionString);

            IEnumerable<CategoryData> parentCategories = categoryDatas.Where(data => !data.ParentId.HasValue);

            var productSearch = _uniquePageService.GetUniquePage<ProductSearch>();
            foreach (CategoryData categoryData in parentCategories)
            {
                UpdateCategory(categoryData, productSearch, categoryDatas, nopImportContext);
            }
            return string.Format("{0} categories processed.", categoryDatas.Count);
        }

        private void UpdateCategory(CategoryData categoryData, Webpage parent, List<CategoryData> allData,
            NopImportContext nopImportContext)
        {
            CategoryData data = categoryData;
            Category category = _session.QueryOver<Category>()
                .Where(c => c.Parent == parent && c.Name == data.Name)
                .List()
                .FirstOrDefault();
            _session.Transact(session =>
            {
                if (category == null)
                {
                    var suggestParams = new SuggestParams
                    {
                        DocumentType = typeof(Category).FullName,
                        PageName = data.Name,
                        UseHierarchy = true
                    };
                    category = new Category
                    {
                        Name = data.Name,
                        UrlSegment = _webpageUrlService.Suggest(parent, suggestParams),
                        Parent = parent
                    };
                    session.Save(category);
                }
                category.Abstract = data.Abstract;
                category.PublishOn = data.Published ? CurrentRequestData.Now.Date : (DateTime?)null;
                session.Update(category);
            });
            nopImportContext.AddEntry(data.Id, category);
            List<CategoryData> children = allData.Where(d => d.ParentId == data.Id).ToList();
            foreach (CategoryData child in children)
            {
                UpdateCategory(child, category, allData, nopImportContext);
            }
        }
    }
}