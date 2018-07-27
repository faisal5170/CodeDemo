//using ECom.Common;
//using ECom.Model.Model;
//using ECom.ViewModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Dynamic;
using System.Text;
using System.Threading.Tasks;

namespace CommerceBit.Repository.Repository.CBRepository
{
    public class ListingTemplateRepository //: RepositoryBase
    {
        private SqlServerSetting _sqlServerSetting;
        public ListingTemplateRepository(string serverName, string userName, string password, string databaseName, bool integratedSecurity)
        : base(serverName, userName, password, databaseName, integratedSecurity)
        {
            _sqlServerSetting = new SqlServerSetting
            {
                ServerName = serverName,
                UserName = userName,
                Password = password,
                DatabaseName = databaseName,
                IntegratedSecurity = integratedSecurity
            };
        }

        public ListingTemplateRepository(SqlServerSetting sqlServerSetting)
        {
            _sqlServerSetting = sqlServerSetting;
        }

        public WebApiResult<object> AddUpdateDescriptionTemplate(ListingTemplateViewModel objDesTemp)
        {
            try
            {
                if (objDesTemp.DescriptionTemplate.DescriptionTemplateId > 0)
                {
                    var dbDescriptionTemplate = GetDescriptionTemplate(objDesTemp.DescriptionTemplate.DescriptionTemplateId);
                    dbDescriptionTemplate.UpdateFrom(objDesTemp.DescriptionTemplate);
                    //Context.Entry(dbDescriptionTemplate).OriginalValues["RowVersion"] = objDesTemp.DescriptionTemplate.RowVersion;
                    var data = Context.EbayItemSpecific.Where(a => a.DesciptionTemplateID == objDesTemp.DescriptionTemplate.DescriptionTemplateId).ToList();
                    Context.EbayItemSpecific.RemoveRange(data);
                    Context.SaveChanges();

                    foreach (var item in objDesTemp.ItemSpecifics)
                    {
                        Ebay_DesciptionTemplate_ItemSpecifics keyval = new Ebay_DesciptionTemplate_ItemSpecifics
                        {
                            DesciptionTemplateID = objDesTemp.DescriptionTemplate.DescriptionTemplateId,
                            ItemKey = item.Key,
                            ItemValue = item.val
                        };
                        Context.EbayItemSpecific.Add(keyval);
                        Context.SaveChanges();
                    }
                    return WebApiResult<object>.New(null, dbDescriptionTemplate.GetVersionJson());
                }
                else
                {
                    try
                    {
                        Context.DescriptionTemplate.Add(objDesTemp.DescriptionTemplate);
                        Context.SaveChanges();
                        foreach (var item in objDesTemp.ItemSpecifics)
                        {
                            Ebay_DesciptionTemplate_ItemSpecifics keyval = new Ebay_DesciptionTemplate_ItemSpecifics
                            {
                                DesciptionTemplateID = objDesTemp.DescriptionTemplate.DescriptionTemplateId,
                                ItemKey = item.Key,
                                ItemValue = item.val
                            };
                            Context.EbayItemSpecific.Add(keyval);
                            Context.SaveChanges();
                        }
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                    return WebApiResult<object>.New(null, objDesTemp.DescriptionTemplate.GetVersionJson());
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public bool AddStoreCategory(StoreCategoryModel objStore)
        {
            try
            {
                if (objStore.StoreCategory1.Count > 0)
                {
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        SqlConnection con = new SqlConnection(Context.Database.Connection.ConnectionString);
                        con.Open();
                        cmd.Connection = con;
                        cmd.CommandText = "TRUNCATE TABLE [EbayStoreCategory1]";
                        cmd.CommandType = CommandType.Text;
                        cmd.ExecuteNonQuery();
                        con.Close();
                    }

                    foreach (var item in objStore.StoreCategory1)
                    {
                        EbayStoreCategory1 cat1 = new EbayStoreCategory1();
                        cat1.CategoryID = item.CategoryID;
                        cat1.EbayMarketplaceID = item.EbayMarketplaceID;
                        cat1.CategoryName = item.CategoryName;
                        cat1.CreatedByUser = item.CreatedByUser;
                        cat1.CreatedByUserId = item.CreatedByUserId;
                        cat1.UpdatedByUser = item.UpdatedByUser;
                        cat1.UpdatedByUserId = item.UpdatedByUserId;
                        Context.EbayStoreCategory1.Add(cat1);
                        Context.SaveChanges();
                    }
                }
                if (objStore.StoreCategory2.Count > 0)
                {
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        SqlConnection con = new SqlConnection(Context.Database.Connection.ConnectionString);
                        con.Open();
                        cmd.Connection = con;
                        cmd.CommandText = "TRUNCATE TABLE [EbayStoreCategory2]";
                        cmd.CommandType = CommandType.Text;
                        cmd.ExecuteNonQuery();
                        con.Close();
                    }
                    foreach (var item in objStore.StoreCategory2)
                    {
                        EbayStoreCategory2 cat2 = new EbayStoreCategory2
                        {
                            CategoryID = item.CategoryID,
                            EbayMarketplaceID = item.EbayMarketplaceID,
                            CategoryName = item.CategoryName,
                            CreatedByUser = item.CreatedByUser,
                            CreatedByUserId = item.CreatedByUserId,
                            UpdatedByUser = item.UpdatedByUser,
                            UpdatedByUserId = item.UpdatedByUserId
                        };
                        Context.EbayStoreCategory2.Add(cat2);
                        Context.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;


        }

        public WebApiResult<object> AddUpdateShippingAndReturnsTemplate(ShippingTemplateViewModel objShipingTemp)
        {
            try
            {
                if (objShipingTemp.ShippingAndReturnsTemplate.ShippingAndReturnsTemplateId > 0)
                {
                    var dbGetShippingAndReturnsTemplate = GetShippingAndReturnsTemplate(objShipingTemp.ShippingAndReturnsTemplate.ShippingAndReturnsTemplateId);
                    dbGetShippingAndReturnsTemplate.UpdateFrom(objShipingTemp.ShippingAndReturnsTemplate);
                    //Context.Entry(dbGetShippingAndReturnsTemplate).OriginalValues["RowVersion"] = objShipingTemp.ShippingAndReturnsTemplate.RowVersion;
                    Context.SaveChanges();
                    foreach (var item in objShipingTemp.ShippingTemplateServices)
                    {
                        if (item.ShipServiceID == 0)
                        {
                            ShippingTemplateServices data = new ShippingTemplateServices
                            {
                                Service = item.Service,
                                Cost = item.Cost,
                                IsfreeShipping = item.IsfreeShipping,
                                DateUpdated = objShipingTemp.ShippingAndReturnsTemplate.DateUpdated,
                                ShippingTemplateID = objShipingTemp.ShippingAndReturnsTemplate.ShippingAndReturnsTemplateId
                            };
                            Context.ShippingTemplateServices.Add(data);
                        }
                        else
                        {
                            var data = GetShippingServicesByID(item.ShipServiceID);
                            data.Service = item.Service;
                            data.Cost = item.Cost;
                            data.IsfreeShipping = item.IsfreeShipping;
                        }
                        Context.SaveChanges();
                    }
                    foreach (var item in objShipingTemp.ShippingTemplateInternationalServices)
                    {
                        if (item.ShipServiceID == 0)
                        {
                            ShippingTemplateInternationalServices data = new ShippingTemplateInternationalServices
                            {
                                Service = item.Service,
                                Cost = item.Cost,
                                IsfreeShipping = item.IsfreeShipping,
                                DateUpdated = objShipingTemp.ShippingAndReturnsTemplate.DateUpdated,
                                ShippingTemplateID = objShipingTemp.ShippingAndReturnsTemplate.ShippingAndReturnsTemplateId
                            };
                            Context.ShippingTemplateInternationalServices.Add(data);
                        }
                        else
                        {
                            var data = GetShippingTemplateInternationalServicesByID(item.ShipServiceID);
                            data.Service = item.Service;
                            data.Cost = item.Cost;
                            data.IsfreeShipping = item.IsfreeShipping;
                        }
                        Context.SaveChanges();
                    }
                    return WebApiResult<object>.New(null, dbGetShippingAndReturnsTemplate.GetVersionJson());
                }
                else
                {
                    try
                    {
                        Context.ShippingAndReturnsTemplate.Add(objShipingTemp.ShippingAndReturnsTemplate);
                        Context.SaveChanges();
                        foreach (var item in objShipingTemp.ShippingTemplateServices)
                        {
                            ShippingTemplateServices service = new ShippingTemplateServices
                            {
                                Service = item.Service,
                                Cost = item.Cost,
                                IsfreeShipping = item.IsfreeShipping,
                                ShippingTemplateID = objShipingTemp.ShippingAndReturnsTemplate.ShippingAndReturnsTemplateId
                            };
                            Context.ShippingTemplateServices.Add(service);
                            Context.SaveChanges();
                        }
                        foreach (var item in objShipingTemp.ShippingTemplateInternationalServices)
                        {
                            ShippingTemplateInternationalServices service = new ShippingTemplateInternationalServices();
                            service.Service = item.Service;
                            service.Cost = item.Cost;
                            service.IsfreeShipping = item.IsfreeShipping;
                            service.ShippingTemplateID = objShipingTemp.ShippingAndReturnsTemplate.ShippingAndReturnsTemplateId;
                            Context.ShippingTemplateInternationalServices.Add(service);
                            Context.SaveChanges();
                        }
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                    return WebApiResult<object>.New(null, objShipingTemp.ShippingAndReturnsTemplate.GetVersionJson());
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public WebApiResult<object> AddUpdateReturnsTemplate(ReturnTemplate objReturnTemp)
        {
            try
            {
                if (objReturnTemp.ReturnTemplateId > 0)
                {
                    var dbReturnsTemplate = GetReturnTemplate(objReturnTemp.ReturnTemplateId);
                    dbReturnsTemplate.UpdateFrom(objReturnTemp);
                    Context.Entry(dbReturnsTemplate).OriginalValues["RowVersion"] = objReturnTemp.RowVersion;
                    Context.SaveChanges();
                    return WebApiResult<object>.New(null, dbReturnsTemplate.GetVersionJson());
                }
                else
                {
                    try
                    {
                        Context.ReturnTemplate.Add(objReturnTemp);
                        Context.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                    return WebApiResult<object>.New(null, objReturnTemp.GetVersionJson());
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public DescriptionTemplate GetDescriptionTemplate(int DescriptionTemplateId)
        {
            return Context.DescriptionTemplate.FirstOrDefault(r => r.DescriptionTemplateId == DescriptionTemplateId);
        }

        public List<Ebay_DesciptionTemplate_ItemSpecifics> GetDescriptionTemplateItemSpecific(int DescriptionTemplateId)
        {
            return Context.EbayItemSpecific.Where(r => r.DesciptionTemplateID == DescriptionTemplateId).ToList();
        }

        public ShippingAndReturnsTemplate GetShippingAndReturnsTemplate(int ShippingAndReturnsTemplateId)
        {
            return Context.ShippingAndReturnsTemplate.FirstOrDefault(r => r.ShippingAndReturnsTemplateId == ShippingAndReturnsTemplateId);
        }

        public ShippingTemplateServices GetShippingServicesByID(int ShippingTemplateServicesID)
        {
            return Context.ShippingTemplateServices.FirstOrDefault(r => r.ShipServiceID == ShippingTemplateServicesID);
        }

        public ShippingTemplateInternationalServices GetShippingTemplateInternationalServicesByID(int ShippingTemplateServicesID)
        {
            return Context.ShippingTemplateInternationalServices.FirstOrDefault(r => r.ShipServiceID == ShippingTemplateServicesID);
        }

        public List<ShippingTemplateServices> GetShippingTemplateServices(int ShippingAndReturnsTemplateId)
        {
            return Context.ShippingTemplateServices.Where(r => r.ShippingTemplateID == ShippingAndReturnsTemplateId).ToList();
        }

        public List<ShippingTemplateInternationalServices> GetShippingTemplateInternationalServices(int ShippingAndReturnsTemplateId)
        {
            return Context.ShippingTemplateInternationalServices.Where(r => r.ShippingTemplateID == ShippingAndReturnsTemplateId).ToList();
        }

        public ReturnTemplate GetReturnTemplate(int ReturnTemplateId)
        {
            return Context.ReturnTemplate.FirstOrDefault(r => r.ReturnTemplateId == ReturnTemplateId);
        }

        public void DeleteDescriptionTemplate(int DescriptionTemplateId)
        {
            var DescriptionTemplate = GetDescriptionTemplate(DescriptionTemplateId);
            Context.DescriptionTemplate.Remove(DescriptionTemplate);
            Context.SaveChanges();
        }
        public void DeleteShippingAndReturnsTemplate(int ShippingAndReturnsTemplateId)
        {
            var ShippingAndReturnsTemplate = GetShippingAndReturnsTemplate(ShippingAndReturnsTemplateId);
            Context.ShippingAndReturnsTemplate.Remove(ShippingAndReturnsTemplate);
            Context.SaveChanges();
        }

        public void DeleteReturnsTemplate(int ReturnsTemplateId)
        {
            var ReturnsTemplate = GetReturnTemplate(ReturnsTemplateId);
            Context.ReturnTemplate.Remove(ReturnsTemplate);
            Context.SaveChanges();
        }

        public List<DescriptionViewModel> GetDescriptionTemplate()
        {
            var data = (from Des in Context.DescriptionTemplate
                        join p in Context.CommerceBitUsers on Des.CreatedByUserId equals p.CommerceBitUserId
                        select new
                        {
                            Des.DescriptionTemplateId,
                            TemplateName = Des.TemplateName,
                            ListingType = Des.ListingType,
                            DateCreated = Des.DateCreated,
                            TotalListingsUsed = Context.ViewListing.Where(a => a.DescriptionTemplate == Des.DescriptionTemplateId).Count(),
                            CreatedByUserId = Des.CreatedByUserId,
                            FullName = p.FirstName + " " + p.LastName
                        }).AsQueryable().Select(c => new DescriptionViewModel()
                        {
                            DescriptionTemplateId = c.DescriptionTemplateId,
                            TemplateName = c.TemplateName,
                            ListingType = c.ListingType,
                            DateCreated = c.DateCreated,
                            TotalListingsUsed = c.TotalListingsUsed,
                            CreatedByUserId = c.CreatedByUserId,
                            FullName = c.FullName
                        }).ToList();
            return data;
            //  return Context.DescriptionTemplate;
        }

        public IQueryable<EbayCategories> GetEbayCategories(int CompanyID)
        {
            return Context.EbayCompanyCategory.Where(a => a.CompanyId == CompanyID);
        }

        public List<EbayCategorySqlModel> GetEbayCategoryForDescription()
        {
            using (CommerceBitDbContext thisContext = new CommerceBitDbContext(_sqlServerSetting))
            {
                thisContext.Configuration.LazyLoadingEnabled = false;
                List<EbayCategorySqlModel> EbayCategory = thisContext.Database.SqlQuery<EbayCategorySqlModel>(@"select * from (select distinct(case when L3.CategoryId is not null then L3.CategoryId
             when L2.CategoryId is not null then L2.CategoryId 
			 else L1.CategoryId end) CategoryId,(L1.CategoryName +
            (case when L1.CategoryId <> L2.CategoryId then + ' > ' + L2.CategoryName  else '' end) +
            (case when L3.CategoryId <> L2.CategoryId then +' > ' + L3.CategoryName  else '' end)   ) CategoryName
            from EbayCategory L1
            Left outer join EbayCategory L2 on L1.CategoryId = l2.CategoryParentId
            Left outer join EbayCategory L3 on L2.CategoryId = L3.CategoryParentId
            where L1.CategoryParentId != L2.CategoryId
            ) A
            where a.CategoryName is not null
            union
            select CategoryId,CategoryName from EbayCategory
             where CategoryId = CategoryParentId
             order by CategoryName").ToList();
                return EbayCategory;
            }

        }

        public List<ShippingTemplatesViewModel> GetShippingAndReturnsTemplate()
        {
            var data = (from Ship in Context.ShippingAndReturnsTemplate
                        join p in Context.CommerceBitUsers on Ship.CreatedByUserId equals p.CommerceBitUserId
                        select new
                        {
                            ShippingAndReturnsTemplateId = Ship.ShippingAndReturnsTemplateId,
                            TemplateName = Ship.TemplateName,
                            ListingType = Ship.ListingType,
                            DateCreated = Ship.DateCreated,
                            TotalListingsUsed = Context.ViewListing.Where(a => a.ShippingTemplate == Ship.ShippingAndReturnsTemplateId).Count(),
                            CreatedByUserId = Ship.CreatedByUserId,
                            FullName = p.FirstName + " " + p.LastName
                        }).AsQueryable().Select(c => new ShippingTemplatesViewModel()
                        {
                            ShippingAndReturnsTemplateId = c.ShippingAndReturnsTemplateId,
                            TemplateName = c.TemplateName,
                            ListingType = c.ListingType,
                            DateCreated = c.DateCreated,
                            TotalListingsUsed = c.TotalListingsUsed,
                            CreatedByUserId = c.CreatedByUserId,
                            FullName = c.FullName
                        }).ToList();
            return data;
        }
        public List<ReturnTemplateViewModel> GetReturnsTemplate()
        {
            var data = (from Ret in Context.ReturnTemplate
                        join p in Context.CommerceBitUsers on Ret.CreatedByUserId equals p.CommerceBitUserId
                        select new
                        {
                            ReturnTemplateId = Ret.ReturnTemplateId,
                            TemplateName = Ret.TemplateName,
                            DateCreated = Ret.DateCreated,
                            TotalListingsUsed = Context.ViewListing.Where(a => a.ReturnTemplate == Ret.ReturnTemplateId).Count(),
                            CreatedByUserId = Ret.CreatedByUserId,
                            FullName = p.FirstName + " " + p.LastName
                        }).AsQueryable().Select(c => new ReturnTemplateViewModel()
                        {
                            ReturnTemplateId = c.ReturnTemplateId,
                            TemplateName = c.TemplateName,
                            DateCreated = c.DateCreated,
                            TotalListingsUsed = c.TotalListingsUsed,
                            CreatedByUserId = c.CreatedByUserId,
                            FullName = c.FullName
                        }).ToList();
            return data;
        }
        public IQueryable<EbayStoreCategories> GetEbayStoreCategories()
        {
            return Context.EbayStoreCategories;
        }

        public WalmartListingTemplate GetWalmartListingTemplate(int WalmartListingId)
        {
            return Context.WalmartListingTemplate.FirstOrDefault(r => r.WalmartListingId == WalmartListingId);
        }

        public void DeleteCategoryWalmartProfiles(int ReturnsTemplateId)
        {
            var ReturnsTemplate = GetWalmartListingTemplate(ReturnsTemplateId);
            Context.WalmartListingTemplate.Remove(ReturnsTemplate);
            Context.SaveChanges();
        }

        public WebApiResult<object> CreateCategoryProfileWal(WalmartListingTemplate objReturnTemp)
        {
            try
            {
                if (objReturnTemp.WalmartListingId > 0)
                {
                    var dbReturnsTemplate = GetWalmartListingTemplate(objReturnTemp.WalmartListingId);
                    dbReturnsTemplate.UpdateFrom(objReturnTemp);
                    //Context.Entry(dbReturnsTemplate).OriginalValues["RowVersion"] = objReturnTemp.RowVersion;
                    Context.SaveChanges();
                    return WebApiResult<object>.New(null, dbReturnsTemplate.GetVersionJson());
                }
                else
                {
                    try
                    {
                        Context.WalmartListingTemplate.Add(objReturnTemp);
                        Context.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                    return WebApiResult<object>.New(null, objReturnTemp.GetVersionJson());
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public List<WalmartTemplateViewModel> GetWalmartCategories()
        {
            return (from Des in Context.WalmartListingTemplate
                    join p in Context.CommerceBitUsers on Des.CreatedByUserId equals p.CommerceBitUserId
                    select new
                    {
                        WalmartListingId = Des.WalmartListingId,
                        ProfileName = Des.ProfileName,
                        Category = Des.Category,
                        SubCategory = Des.SubCategory,
                        DateCreated = Des.DateCreated,
                        TotalListingsUsed = Context.ListingWalMarts.Where(a => a.ListingWalMartId == Des.WalmartListingId).Count(),
                        CreatedByUserId = Des.CreatedByUserId,
                        FullName = p.FirstName + " " + p.LastName
                    }).AsQueryable().Select(c => new WalmartTemplateViewModel()
                    {
                        WalmartListingId = c.WalmartListingId,
                        ProfileName = c.ProfileName,
                        Category = c.Category,
                        SubCategory = c.SubCategory,
                        DateCreated = c.DateCreated,
                        TotalListingsUsed = c.TotalListingsUsed,
                        CreatedByUserId = c.CreatedByUserId,
                        FullName = c.FullName
                    }).ToList();
        }

        public List<NewEggTemplateViewModel> GetNewEggCategories()
        {
            return (from Des in Context.NewEggListingTemplate
                    join p in Context.CommerceBitUsers on Des.CreatedByUserId equals p.CommerceBitUserId
                    select new
                    {
                        NewEggListingId = Des.NewEggListingId,
                        ProfileName = Des.ProfileName,
                        Category = Des.Category,
                        SubCategory = Des.SubCategory,
                        DateCreated = Des.DateCreated,
                        TotalListingsUsed = 0,
                        CreatedByUserId = Des.CreatedByUserId,
                        FullName = p.FirstName + " " + p.LastName
                    }).AsQueryable().Select(c => new NewEggTemplateViewModel()
                    {
                        NewEggListingId = c.NewEggListingId,
                        ProfileName = c.ProfileName,
                        Category = c.Category,
                        SubCategory = c.SubCategory,
                        DateCreated = c.DateCreated,
                        TotalListingsUsed = c.TotalListingsUsed,
                        CreatedByUserId = c.CreatedByUserId,
                        FullName = c.FullName
                    }).ToList();
        }

        public WebApiResult<object> CreateCategoryProfileNew(NewEggListingTemplate objReturnTemp)
        {
            try
            {
                if (objReturnTemp.NewEggListingId > 0)
                {
                    var dbReturnsTemplate = GetNewEggListingTemplate(objReturnTemp.NewEggListingId);
                    dbReturnsTemplate.UpdateFrom(objReturnTemp);
                    Context.SaveChanges();
                    return WebApiResult<object>.New(null, dbReturnsTemplate.GetVersionJson());
                }
                else
                {
                    try
                    {
                        Context.NewEggListingTemplate.Add(objReturnTemp);
                        Context.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                    return WebApiResult<object>.New(null, objReturnTemp.GetVersionJson());
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public NewEggListingTemplate GetNewEggListingTemplate(int NewEggListingId)
        {
            return Context.NewEggListingTemplate.FirstOrDefault(r => r.NewEggListingId == NewEggListingId);
        }

        public void DeleteCategoryNewEggProfiles(int ReturnsTemplateId)
        {
            var ReturnsTemplate = GetNewEggListingTemplate(ReturnsTemplateId);
            Context.NewEggListingTemplate.Remove(ReturnsTemplate);
            Context.SaveChanges();
        }

        public List<TangaTemplateViewModel> GetTangaCategories()
        {
            return (from Des in Context.TangaListingTemplate
                    join p in Context.CommerceBitUsers on Des.CreatedByUserId equals p.CommerceBitUserId
                    select new
                    {
                        TangaListingTemplateId = Des.TangaListingTemplateId,
                        ProfileName = Des.ProfileName,
                        Category = Des.Category,
                        DateCreated = Des.DateCreated,
                        TotalListingsUsed = Context.TangaViewListing.Where(a => a.TangaViewListingId == Des.TangaListingTemplateId).Count(),
                        CreatedByUserId = Des.CreatedByUserId,
                        FullName = p.FirstName + " " + p.LastName
                    }).AsQueryable().Select(c => new TangaTemplateViewModel()
                    {
                        TangaListingTemplateId = c.TangaListingTemplateId,
                        ProfileName = c.ProfileName,
                        Category = c.Category,
                        DateCreated = c.DateCreated,
                        TotalListingsUsed = c.TotalListingsUsed,
                        CreatedByUserId = c.CreatedByUserId,
                        FullName = c.FullName
                    }).ToList();
        }
        public TangaListingTemplate GetTangaListingTemplate(int TangaListingId)
        {
            return Context.TangaListingTemplate.FirstOrDefault(r => r.TangaListingTemplateId == TangaListingId);
        }

        public WebApiResult<object> CreateCategoryProfileTang(TangaListingTemplate objReturnTemp)
        {
            try
            {
                if (objReturnTemp.TangaListingTemplateId > 0)
                {
                    var dbReturnsTemplate = GetTangaListingTemplate(objReturnTemp.TangaListingTemplateId);
                    dbReturnsTemplate.UpdateFrom(objReturnTemp);
                    Context.SaveChanges();
                    return WebApiResult<object>.New(null, dbReturnsTemplate.GetVersionJson());
                }
                else
                {
                    try
                    {
                        Context.TangaListingTemplate.Add(objReturnTemp);
                        Context.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                    return WebApiResult<object>.New(null, objReturnTemp.GetVersionJson());
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public void DeleteCategoryTangaProfiles(int ReturnsTemplateId)
        {
            var ReturnsTemplate = GetTangaListingTemplate(ReturnsTemplateId);
            Context.TangaListingTemplate.Remove(ReturnsTemplate);
            Context.SaveChanges();
        }
        public WebApiResult<object> CreateCategoryProfileETS(ETSYListingTemplate objReturnTemp)
        {
            try
            {
                if (objReturnTemp.ETSYListingTemplateId > 0)
                {
                    var dbReturnsTemplate = GetETSYListingTemplate(objReturnTemp.ETSYListingTemplateId);
                    dbReturnsTemplate.UpdateFrom(objReturnTemp);
                    Context.SaveChanges();
                    return WebApiResult<object>.New(null, dbReturnsTemplate.GetVersionJson());
                }
                else
                {
                    try
                    {
                        Context.ETSYListingTemplate.Add(objReturnTemp);
                        Context.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                    return WebApiResult<object>.New(null, objReturnTemp.GetVersionJson());
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public ETSYListingTemplate GetETSYListingTemplate(int ETSYListingId)
        {
            return Context.ETSYListingTemplate.FirstOrDefault(r => r.ETSYListingTemplateId == ETSYListingId);
        }
        public void DeleteCategoryETSYProfiles(int ReturnsTemplateId)
        {
            var ReturnsTemplate = GetETSYListingTemplate(ReturnsTemplateId);
            Context.ETSYListingTemplate.Remove(ReturnsTemplate);
            Context.SaveChanges();
        }
        public List<ETSYListingTemplate> GetETSYCategories()
        {
            return Context.ETSYListingTemplate.ToList();
        }
        public List<EtsyPrimaryCategory> GetETSYPrimaryCategories()
        {
            return Context.EtsyPrimaryCategory.ToList();
        }
        public List<EtsySubCategory> GetETSYListingSecondTemplate(int id)
        {
            return Context.EtsySubCategory.Where(r => r.PrimaryId == id).ToList();
        }
        public List<EtsySubSubCategory> GetETSYListingSecondSubTemplate(int id)
        {
            return Context.EtsySubSubCategory.Where(r => r.SubId == id).ToList();
        }
        public JETListingTemplate GetJETListingTemplate(int JETListingId)
        {
            return Context.JETListingTemplate.FirstOrDefault(r => r.JETListingId == JETListingId);
        }
        public WebApiResult<object> JETCreateCategoryProfile(JETListingTemplate objReturnTemp)
        {
            try
            {
                if (objReturnTemp.JETListingId > 0)
                {
                    var dbReturnsTemplate = GetJETListingTemplate(objReturnTemp.JETListingId);
                    dbReturnsTemplate.UpdateFrom(objReturnTemp);
                    Context.SaveChanges();
                    return WebApiResult<object>.New(null, dbReturnsTemplate.GetVersionJson());
                }
                else
                {
                    try
                    {
                        Context.JETListingTemplate.Add(objReturnTemp);
                        Context.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                    return WebApiResult<object>.New(null, objReturnTemp.GetVersionJson());
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public List<JETTemplateViewModel> GetJETCategories()
        {
            return (from Des in Context.JETListingTemplate
                    join p in Context.CommerceBitUsers on Des.CreatedByUserId equals p.CommerceBitUserId
                    select new
                    {
                        JETListingId = Des.JETListingId,
                        ProfileName = Des.ProfileName,
                        Category = Des.Category,
                        DateCreated = Des.DateCreated,
                        TotalListingsUsed = 0,
                        CreatedByUserId = Des.CreatedByUserId,
                        FullName = p.FirstName + " " + p.LastName
                    }).AsQueryable().Select(c => new JETTemplateViewModel()
                    {
                        JETListingId = c.JETListingId,
                        ProfileName = c.ProfileName,
                        Category = c.Category,
                        DateCreated = c.DateCreated,
                        TotalListingsUsed = c.TotalListingsUsed,
                        CreatedByUserId = c.CreatedByUserId,
                        FullName = c.FullName
                    }).ToList();
        }
        public void DeleteCategoryJETProfiles(int ReturnsTemplateId)
        {
            var ReturnsTemplate = GetJETListingTemplate(ReturnsTemplateId);
            Context.JETListingTemplate.Remove(ReturnsTemplate);
            Context.SaveChanges();
        }
    }
}