//using ECom.Common;
//using ECom.Common.Api.ApiResponse;
//using ECom.Common.Api.ApiUtil;
//using ECom.Model.Model;
//using ECom.Model.Security;
//using ECom.ViewModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace CustomCodingStyle.Controllers
{
    public class InventoryController// : AdminBaseController
    {
        // GET: Admin/Inventory
        public ActionResult Index(string search, int? page, int? pagesize)
        {
            dbPageId = 34;
            int? PageSize;
            if (pagesize == null)
                PageSize = 50;
            else
                PageSize = pagesize;
            ViewBag.PageSize = PageSize;
            var model = new InventoryOverView()
            {
                InventoryFilter = new InventoryFilter()
            };
            if (string.IsNullOrEmpty(search)) search = "nosearch";
            InventoryApiResponse inventoryApiResponse = InventoryApiUtil.GetInventoryOverViewList(CurrentCompanyId, search, page == null ? 0 : page.Value, PageSize).Result;
            model = inventoryApiResponse.InventoryOverView;
            if (model == null)
            {
                model = new InventoryOverView()
                {
                    //InventoryPageList = new InventoryOverView()
                    //{
                    //    data = new List<Inventory>()
                    //}
                };
            }
            model.CurrentPage = page == null ? 0 : page.Value;

            model.InventoryFilter = new InventoryFilter
            {
                BrandList = new List<SelectListItem>(),
                ManufactureList = new List<SelectListItem>()
            };
            if (model.Inventories != null)
            {
                var brandlst = model.Inventories.Where(f => !string.IsNullOrEmpty(f.Brand)).Distinct().ToList();
                var manufalst = model.Inventories.Where(f => !string.IsNullOrEmpty(f.Manufacture)).Distinct().ToList();
                if (brandlst != null)
                    model.InventoryFilter.BrandList = brandlst.Select(p => new SelectListItem { Text = p.Brand, Value = p.Brand }).ToList();
                if (manufalst != null)
                    model.InventoryFilter.ManufactureList = manufalst.Select(p => new SelectListItem { Text = p.Manufacture, Value = p.Manufacture }).ToList();
                //model.InventoryFilter.BrandList = GetBrandList().Select(p => new SelectListItem { Text = p.Name, Value = p.Name }).ToList();
                //model.InventoryFilter.ManufactureList = GetManufacturerList().Select(p => new SelectListItem { Text = p.Name, Value = p.Name }).ToList();
            }

            MarketPlaceSettingsApiResponse AmazonResponse = MarketPlaceSettingsApiUtil.GetAllAmazonMWSSettingRecords(CurrentCompanyId).Result;
            if (AmazonResponse.IsSuccessful)
            {
                model.AmazonMWSSetting = AmazonResponse.AmazonMWSSettings;
                Session["AmazonMWSSetting"] = model.AmazonMWSSetting;
            }
            else
            {
                model.AmazonMWSSetting = new List<AmazonMWSSetting>();
                Session["AmazonMWSSetting"] = new List<AmazonMWSSetting>();
            }
            MarketPlaceSettingsApiResponse WalmartResponse = MarketPlaceSettingsApiUtil.GetAllWalMartRecords(CurrentCompanyId).Result;
            if (WalmartResponse.IsSuccessful)
            {
                model.WalMartSetting = WalmartResponse.WalMartList;
                Session["WalmartSetting"] = model.WalMartSetting;
            }
            else
            {
                model.WalMartSetting = new List<WalMartSetting>();
                Session["WalmartSetting"] = new List<WalMartSetting>();
            }
            model.InventoryFilter.UserList = new List<SelectListItem>();
            model.InventoryFilter.UserList = GetUsers().Select(p => new SelectListItem { Text = p.FullName, Value = p.FullName }).ToList();

            model.InventoryFilter.WarehouseList = new List<SelectListItem>();
            WarehouseApiResponse warehouseApiResponse = WarehouseApiUtil.GetAllWarehouseRecords(CurrentCompanyId).Result;
            if (warehouseApiResponse.Warehouses != null)
                model.InventoryFilter.WarehouseList = warehouseApiResponse.Warehouses.Select(p => new SelectListItem { Text = p.WarehouseName, Value = p.WarehouseName }).ToList();

            ViewBag.IsOpenInNewTab = false;
            var generalSettingResultLink = GeneralSettingApiUtil.GetGeneralSettingByKey(CurrentCompanyId, "inventory", "Open Product Details Page in New Window").Result;
            if (generalSettingResultLink.GeneralSetting != null)
                if (generalSettingResultLink.GeneralSetting.SettingValue != null)
                    ViewBag.IsOpenInNewTab = Convert.ToBoolean(generalSettingResultLink.GeneralSetting.SettingValue);

            if (model != null)
                return View("Index", model);
            else
                return View(new InventoryOverView());
        }

        public ActionResult CreateSKUFromASIN()
        {
            dbPageId = 34;
            var Data = Session["AmazonMWSSetting"] as List<AmazonMWSSetting>;
            if (Data == null)
            {
                MarketPlaceSettingsApiResponse AmazonResponse = MarketPlaceSettingsApiUtil.GetAllAmazonMWSSettingRecords(CurrentCompanyId).Result;
                if (AmazonResponse.IsSuccessful)
                    Data = AmazonResponse.AmazonMWSSettings;
            }
            ViewBag.AmazonMWSSetting = Data;
            return View();
        }

        public ActionResult FetchAmazonDataFromAPI(string Query, int AmazonID)
        {
            List<ProductModel> response = new List<ProductModel>();
            try
            {
                dbPageId = 34;
                MarketPlaceSettingsApiResponse Response = MarketPlaceSettingsApiUtil.GetAmazonMWSSettingRecord(CurrentCompanyId, AmazonID).Result;
                var AmazonSetting = Response.AmazonMWSSetting;
                var objMWS = new AmazonMWS.MWS(AmazonSetting.AWSAccessKeyID, AmazonSetting.AmazonSecretKey, AmazonSetting.AmazonSellerID);
                response = objMWS.ListMatchingProduct(Query, AmazonSetting.AmazonMarketplaceID);
            }
            catch (Exception ex)
            {
            }
            return PartialView(response);
        }

        public ActionResult Create()
        {
            dbPageId = 34;
            InventoryViewModel model = new InventoryViewModel()
            {
                Inventory = new Inventory(),
                WarehouseInventories = new List<WarehouseInventory>(),
                Inventories = new List<Inventory>(),
                BrandList = new List<SelectListItem>(),
                ManufactureList = new List<SelectListItem>()
            };

            if (Session["SortReceiveNewSKU"] != null)
            {
                model.Inventory = new Inventory() { Condition = Session["SortReceiveNewSKU"].ToString() };
                Session["SortReceiveNewSKU"] = null;
            }

            //Get Warehouses
            model.WarehouseInventories = GetWarehouseInventories(0);
            model.CustomFieldInventories = GetCustomFieldInventories(0);
            model.BrandList = GetBrandList().Select(p => new SelectListItem { Text = p.Name, Value = p.Name }).ToList();
            model.ManufactureList = GetManufacturerList().Select(p => new SelectListItem { Text = p.Name, Value = p.Name }).ToList();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(InventoryViewModel model)
        {
            dbPageId = 34;
            if (CurrentBaseCompanyId == CurrentCompanyId)
            {
                model.Inventory.CreatedByUserId = CurrentUserId;
                model.Inventory.UpdatedByUserId = CurrentUserId;
            }
            else
            {
                model.Inventory.CreatedByUserId = SystemUserId;
                model.Inventory.UpdatedByUserId = SystemUserId;
            }

            if (ModelState.IsValid)
            {
                model.Inventory.InventoryImages = new List<InventoryImage>();
                foreach (HttpPostedFileBase file in model.files)
                {
                    //Checking file is available to save.  
                    if (file != null)
                    {
                        var Ext = Path.GetExtension(file.FileName);
                        var inputFileName = "Image1" + Ext;
                        System.Drawing.Image img = System.Drawing.Image.FromStream(file.InputStream);
                        int height = img.Height;
                        int width = img.Width;
                        //FilePath = "~/Uploads/" + CurrentCompanyId + "/InventoryImages/" + InventoryId + "/";

                        //var inputFileName = Path.GetFileName(file.FileName);
                        var filePath = "~/Uploads/" + CurrentCompanyId + "/InventoryImages/";
                        var directoryPath = Server.MapPath(filePath);
                        if (!Directory.Exists(directoryPath)) //If No any such directory then creates the new one
                        {
                            Directory.CreateDirectory(directoryPath);
                        }
                        var serverSavePath = Path.Combine(directoryPath + inputFileName);
                        //Save file to server folder  
                        file.SaveAs(serverSavePath);
                        filePath = filePath + inputFileName;
                        //assigning file uploaded to InventoryImage Collection  
                        if (CurrentBaseCompanyId == CurrentCompanyId)
                        {
                            model.Inventory.InventoryImages.Add(new InventoryImage { ImageName = inputFileName, ImagePath = filePath, ImageWidth = width, ImageHeight = height, Status = "A", CreatedByUserId = CurrentUserId, UpdatedByUserId = CurrentUserId });
                        }
                        else
                        {
                            model.Inventory.InventoryImages.Add(new InventoryImage { ImageName = inputFileName, ImagePath = filePath, ImageWidth = width, ImageHeight = height, Status = "A", CreatedByUserId = SystemUserId, UpdatedByUserId = SystemUserId });
                        }

                    }
                }
                model.Inventory.WarehouseInventories = new List<WarehouseInventory>();
                if (model.WarehouseInventories != null)
                {
                    foreach (var warehouseInventory in model.WarehouseInventories)
                    {
                        warehouseInventory.Warehouse = null;
                        if (CurrentBaseCompanyId == CurrentCompanyId)
                        {
                            warehouseInventory.CreatedByUserId = CurrentUserId;
                            warehouseInventory.UpdatedByUserId = CurrentUserId;
                        }
                        else
                        {
                            warehouseInventory.CreatedByUserId = SystemUserId;
                            warehouseInventory.UpdatedByUserId = SystemUserId;
                        }
                        model.Inventory.WarehouseInventories.Add(warehouseInventory);
                    }
                }
                model.Inventory.InventoryAliases = new List<InventoryAlias>();
                if (model.Alias != null)
                {
                    foreach (var alias in model.Alias)
                    {
                        if (string.IsNullOrEmpty(alias.SKU)) continue;
                        if (CurrentBaseCompanyId == CurrentCompanyId)
                        {
                            alias.CreatedByUserId = CurrentUserId;
                            alias.UpdatedByUserId = CurrentUserId;
                        }
                        else
                        {
                            alias.CreatedByUserId = SystemUserId;
                            alias.UpdatedByUserId = SystemUserId;
                        }
                        model.Inventory.InventoryAliases.Add(alias);
                    }
                }
                model.Inventory.InventoryKits = new List<InventoryKit>();
                if (model.Kits != null)
                {
                    foreach (var kit in model.Kits)
                    {
                        if (string.IsNullOrEmpty(kit.InventoryId.ToString()) ||
                            string.IsNullOrEmpty(kit.QTY.ToString())) continue;
                        if (CurrentBaseCompanyId == CurrentCompanyId)
                        {
                            kit.CreatedByUserId = CurrentUserId;
                            kit.UpdatedByUserId = CurrentUserId;
                        }
                        else
                        {
                            kit.CreatedByUserId = SystemUserId;
                            kit.UpdatedByUserId = SystemUserId;
                        }
                        model.Inventory.InventoryKits.Add(kit);
                    }
                }
                model.Inventory.ProductMarkers = new List<ProductMarker>();
                if (model.ProductMarkers != null)
                {
                    foreach (var marker in model.ProductMarkers)
                    {

                        if (CurrentBaseCompanyId == CurrentCompanyId)
                        {
                            marker.CreatedByUserId = CurrentUserId;
                            marker.UpdatedByUserId = CurrentUserId;
                        }
                        else
                        {
                            marker.CreatedByUserId = SystemUserId;
                            marker.UpdatedByUserId = SystemUserId;
                        }
                        model.Inventory.ProductMarkers.Add(marker);
                    }
                }
                WebApiResponse webApiResponse = InventoryApiUtil.AddInventory(CurrentCompanyId, model.Inventory).Result;
                if (webApiResponse.IsSuccessful)
                {
                    ViewBag.Message = ShowMessage(MessageType.success, $"Product {model.Inventory.SKU} added successfully.");
                    return RedirectToAction("Index");
                }
                else
                {
                    ViewBag.Message = ShowMessage(MessageType.danger, webApiResponse.Message);
                    //Get Inventories
                    model.Inventories = new List<Inventory>();
                    model.Inventories = GetInventories();
                    //Get KitTypes
                    model.KitTypes = GetKitTypes();
                    //Get Warehouses
                    model.Warehouses = GetWarehouseLists();
                    //Get Warehouses
                    model.WarehouseInventories = GetWarehouseInventories(0);
                    model.BrandList = new List<SelectListItem>();
                    model.ManufactureList = new List<SelectListItem>();
                    model.BrandList = GetBrandList().Select(p => new SelectListItem { Text = p.Name, Value = p.Name }).ToList();
                    model.ManufactureList = GetManufacturerList().Select(p => new SelectListItem { Text = p.Name, Value = p.Name }).ToList();
                    return View(model);
                }
            }

            var errorMessage = "";
            foreach (ModelState modelState in ViewData.ModelState.Values)
            {
                foreach (ModelError error in modelState.Errors)
                {
                    errorMessage = errorMessage + error.ErrorMessage.ToString();
                }
            }

            ViewBag.Message = ShowMessage(MessageType.danger, errorMessage);
            //Get Inventories
            model.Inventories = new List<Inventory>();
            model.Inventories = GetInventories();
            //Get KitTypes
            model.KitTypes = GetKitTypes();
            //Get Warehouses
            model.Warehouses = GetWarehouseLists();
            //Get Warehouses
            model.WarehouseInventories = GetWarehouseInventories(0);
            model.BrandList = new List<SelectListItem>();
            model.ManufactureList = new List<SelectListItem>();
            model.BrandList = GetBrandList().Select(p => new SelectListItem { Text = p.Name, Value = p.Name }).ToList();
            model.ManufactureList = GetManufacturerList().Select(p => new SelectListItem { Text = p.Name, Value = p.Name }).ToList();

            return View(model);
        }
        public ActionResult Detail(string productid)
        {
            dbPageId = 34;
            int id = Convert.ToInt32(productid);
            var inventoryApiResponse = InventoryApiUtil.GetInventoryRecord(CurrentCompanyId, id).Result;
            return View(inventoryApiResponse.Inventory);
        }
        public ActionResult Edit(string productid)
        {
            dbPageId = 34;
            int id = Convert.ToInt32(productid);
            var model = new InventoryViewModel
            {
                InventoryImages = new List<InventoryImage>(),
                Alias = new List<InventoryAlias>(),
                Kits = new List<InventoryKit>(),
                Orders = new List<Order>(),
                PurchaseOrders = new List<PurchaseOrder>(),
                ProductMarkers = new List<ProductMarker>(),
                InventoryVariations = new List<InventoryVariation>(),
                InventorySKULogs = new List<InventorySKULog>(),
                WarehouseInventories = new List<WarehouseInventory>(),
                WarehouseTransfers = new List<WarehouseTransfer>(),
                OtherCurrencyPrices = new List<InventoryOtherCurrencyPrice>(),
                InventoryBuyers = new List<InventoryBuyer>(),
                Users = new List<CommerceBitUser>(),
                InventoryVendorSKUs = new List<InventoryVendorSKU>(),
                BrandList = new List<SelectListItem>(),
                ManufactureList = new List<SelectListItem>(),
                MarketPlaceListings = new List<MarketPlaceListing>(),
                CustomFieldsList = new List<CustomFields>(),
                CustomFieldInventories = new List<CustomFieldInventory>()
            };

            ViewBag.IsViewMarketPlaceListing = false;
            var generalSettingResult = GeneralSettingApiUtil.GetGeneralSettingByKey(CurrentCompanyId, "inventory", "Show Marketplace Table in Edit Inventory Page").Result;
            if (generalSettingResult.GeneralSetting != null)
                if (generalSettingResult.GeneralSetting.SettingValue != null)
                    ViewBag.IsViewMarketPlaceListing = Convert.ToBoolean(generalSettingResult.GeneralSetting.SettingValue);

            ViewBag.IsOpenInNewTab = false;
            var generalSettingResultLink = GeneralSettingApiUtil.GetGeneralSettingByKey(CurrentCompanyId, "inventory", "Open Product Details Page in New Window").Result;
            if (generalSettingResultLink.GeneralSetting != null)
                if (generalSettingResultLink.GeneralSetting.SettingValue != null)
                    ViewBag.IsOpenInNewTab = Convert.ToBoolean(generalSettingResultLink.GeneralSetting.SettingValue);

            try
            {
                var inventoryApiResponse = InventoryApiUtil.GetInventoryRecord(CurrentCompanyId, id).Result;
                if (inventoryApiResponse.Inventory != null)
                {
                    model.Inventory = inventoryApiResponse.Inventory;
                    if (model.Inventory.InventoryImages != null && model.Inventory.InventoryImages.Count > 0)
                    {
                        model.InventoryImages.AddRange(model.Inventory.InventoryImages);
                    }

                    if (model.Inventory.InventoryAliases != null && model.Inventory.InventoryAliases.Count > 0)
                    {
                        model.Alias.AddRange(model.Inventory.InventoryAliases);
                    }

                    if (model.Inventory.InventoryKits != null && model.Inventory.InventoryKits.Count > 0)
                    {
                        model.Kits.AddRange(model.Inventory.InventoryKits);
                    }

                    if (model.Inventory.ProductMarkers != null && model.Inventory.ProductMarkers.Count > 0)
                    {
                        model.ProductMarkers.AddRange(model.Inventory.ProductMarkers);
                    }

                    if (model.Inventory.InventoryVariations != null && model.Inventory.InventoryVariations.Count > 0)
                    {
                        model.InventoryVariations.AddRange(model.Inventory.InventoryVariations);
                    }

                    if (model.Inventory.InventorySKULogs != null && model.Inventory.InventorySKULogs.Count > 0)
                    {
                        model.InventorySKULogs.AddRange(model.Inventory.InventorySKULogs);
                    }

                    if (model.Inventory.OtherCurrencyPrices != null && model.Inventory.OtherCurrencyPrices.Count > 0)
                    {
                        model.OtherCurrencyPrices.AddRange(model.Inventory.OtherCurrencyPrices);
                    }

                    if (model.Inventory.InventoryBuyers != null && model.Inventory.InventoryBuyers.Count > 0)
                    {
                        model.InventoryBuyers.AddRange(model.Inventory.InventoryBuyers);
                    }

                    if (model.Inventory.InventoryVendorSKUs != null && model.Inventory.InventoryVendorSKUs.Count > 0)
                    {
                        model.InventoryVendorSKUs.AddRange(model.Inventory.InventoryVendorSKUs);
                    }
                    
                    model.Inventory.Weight = (model.Inventory.IsOZorLBS == false ? model.Inventory.WeightLBS : model.Inventory.WeightOZ);

                    var generalSettingResultForceForCommentsWhenEditQTY = GeneralSettingApiUtil.GetGeneralSettingByKey(CurrentCompanyId, "inventory", "Force User To Enter Comments When Setting QTY").Result;
                    if (generalSettingResultForceForCommentsWhenEditQTY.GeneralSetting != null)
                        if (generalSettingResultForceForCommentsWhenEditQTY.GeneralSetting.SettingValue != null)
                            model.Inventory.IsForceForCommentsWhenEditQTY = Convert.ToBoolean(generalSettingResultForceForCommentsWhenEditQTY.GeneralSetting.SettingValue);
                }

                //model.Inventory = inventoryApiResponse.Inventory;
                //Get Orders
                model.Orders = GetOrderRecordsByInventoryId(id, 1, 5);
                //Get Orders
                model.PurchaseOrders = GetPurchaseOrderRecordsByInventoryId(id, 1, 15);

                //Get Inventories
                model.Inventories = new List<Inventory>();
                model.Inventories = GetInventories();

                //Get Users
                model.Users = GetUsers();

                //Get KitTypes
                model.KitTypes = GetKitTypes();
                //Get Warehouses
                model.Warehouses = GetWarehouseLists();
                //Get Warehouses
                model.WarehouseInventories = GetWarehouseInventories(id); //warehouselist.Join(inventorywarehouseList, w => w.Warehouse.WarehouseId, i => i.WarehouseId, (w, i) => new { warehouselist = w, inventorywarehouseList = i }).Select(x => new WarehouseInventory { WarehouseInventoryId = x.warehouselist.WarehouseInventoryId, WarehouseId = x.warehouselist.WarehouseId, InventoryId = x.warehouselist.InventoryId, Warehouse = x.warehouselist.Warehouse, Quantity = x.warehouselist.Quantity, ProductLocation = x.warehouselist.ProductLocation } ).ToList(); //.Select(x => x.inventorywarehouseList).ToList()
                                                                          //Get Warehouses Transfer
                model.WarehouseTransfers = GetWarehouseTransfers(id);
                model.BrandList = GetBrandList().Select(p => new SelectListItem { Text = p.Name, Value = p.Name }).ToList();
                model.ManufactureList = GetManufacturerList().Select(p => new SelectListItem { Text = p.Name, Value = p.Name }).ToList();
                //Get CustomFields
                model.CustomFieldsList = GetCustomFieldLists();
                //Get CustomFields Inventories
                model.CustomFieldInventories = GetCustomFieldInventories(id);
                Inventory objparam = InventoryApiUtil.GetOnlyInventoryRecord(CurrentCompanyId, Convert.ToInt32(productid)).Result.Inventory;
                var sku = objparam.SKU.Replace('.', '_');
                var objparamMarket = InventoryApiUtil.GetMarketPlaceListingRecord(CurrentCompanyId, sku).Result;
                model.MarketPlaceListings = objparamMarket.MarketPlaceListing;
            }
            catch
            {
            }

            if (TempData["displayalert"] != null)
            {
                if (Convert.ToBoolean(TempData["displayalert"]) == true)
                    ViewBag.Message = ShowMessage(MessageType.success, TempData["alert"].ToString());
                else if (Convert.ToBoolean(TempData["displayalert"]) == false)
                {
                    ViewBag.Message = ShowMessage(MessageType.danger, TempData["alert"].ToString());
                    if (TempData["displaymodel"] != null)
                        return View((InventoryViewModel)TempData["displaymodel"]);

                }
            }
            return View(model);
        }

        private List<CommerceBitUser> GetUsers()
        {
            UserApiResponse userApiResponse = UserApiUtil.GetUsers(CurrentCompanyId).Result;
            if (userApiResponse.CommerceBitUsers != null)
                return userApiResponse.CommerceBitUsers;

            return new List<CommerceBitUser>();
        }

        [HttpPost]
        public JsonResult CreateFromAmazonAPI(InventoryViewModel model)
        {
            dbPageId = 34;
            if (CurrentBaseCompanyId == CurrentCompanyId)
            {
                model.Inventory.CreatedByUserId = CurrentUserId;
                model.Inventory.UpdatedByUserId = CurrentUserId;
            }
            else
            {
                model.Inventory.CreatedByUserId = SystemUserId;
                model.Inventory.UpdatedByUserId = SystemUserId;
            }

            if (ModelState.IsValid)
            {
                if (model.AmazonImagePath != null && model.AmazonImagePath != "")
                {
                    model.Inventory.InventoryImages = new List<InventoryImage>();
                    var Ext = Path.GetExtension(model.AmazonImagePath);
                    var inputFileName = "Image_" + DateTime.Now.ToString("X") + Ext;
                    var filePath = "~/Uploads/" + CurrentCompanyId + "/InventoryImages/AmazonAPI_Images/" + model.AmazonProductMarker.MarkerValue + "/";
                    var directoryPath = Server.MapPath(filePath);
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }
                    var serverSavePath = Path.Combine(directoryPath + inputFileName);
                    using (WebClient wc = new WebClient())
                    {
                        wc.DownloadFile(model.AmazonImagePath, serverSavePath);
                    }
                    System.Drawing.Image img = System.Drawing.Image.FromFile(serverSavePath);
                    int height = img.Height;
                    int width = img.Width;
                    string ext = Path.GetExtension(serverSavePath);
                    filePath = filePath + inputFileName;
                    //assigning file uploaded to InventoryImage Collection  
                    if (CurrentBaseCompanyId == CurrentCompanyId)
                    {
                        model.Inventory.InventoryImages.Add(new InventoryImage { ImageName = inputFileName, ImagePath = filePath, ImageWidth = width, ImageHeight = height, Status = "A", CreatedByUserId = CurrentUserId, UpdatedByUserId = CurrentUserId });
                    }
                    else
                    {
                        model.Inventory.InventoryImages.Add(new InventoryImage { ImageName = inputFileName, ImagePath = filePath, ImageWidth = width, ImageHeight = height, Status = "A", CreatedByUserId = SystemUserId, UpdatedByUserId = SystemUserId });
                    }
                }

                model.Inventory.ProductMarkers = new List<ProductMarker>();
                if (model.AmazonProductMarker != null)
                {
                    var marker = model.AmazonProductMarker;
                    if (CurrentBaseCompanyId == CurrentCompanyId)
                    {
                        marker.CreatedByUserId = CurrentUserId;
                        marker.UpdatedByUserId = CurrentUserId;
                    }
                    else
                    {
                        marker.CreatedByUserId = SystemUserId;
                        marker.UpdatedByUserId = SystemUserId;
                    }
                    model.Inventory.ProductMarkers.Add(marker);

                }
                WebApiResponse webApiResponse = InventoryApiUtil.AddInventory(CurrentCompanyId, model.Inventory).Result;
                if (webApiResponse.IsSuccessful)
                {
                    return Json(true, JsonRequestBehavior.AllowGet);
                }
            }
            return Json(true, JsonRequestBehavior.AllowGet);
            // return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(InventoryViewModel model)
        {
            dbPageId = 34;
            if (model.Inventory.IsOZorLBS)
            {
                model.Inventory.WeightOZ = model.Inventory.Weight;
                model.Inventory.WeightLBS = 0;
            }
            else
            {
                model.Inventory.WeightLBS = model.Inventory.Weight;
                model.Inventory.WeightOZ = 0;
            }

            if (CurrentBaseCompanyId == CurrentCompanyId)
            {
                model.Inventory.CreatedByUserId = CurrentUserId;
                model.Inventory.UpdatedByUserId = CurrentUserId;
            }
            else
            {
                model.Inventory.CreatedByUserId = SystemUserId;
                model.Inventory.UpdatedByUserId = SystemUserId;
            }

            ViewBag.IsViewMarketPlaceListing = false;
            var generalSettingResult = GeneralSettingApiUtil.GetGeneralSettingByKey(CurrentCompanyId, "inventory", "Show Marketplace Table in Edit Inventory Page").Result;
            if (generalSettingResult.GeneralSetting != null)
                if (generalSettingResult.GeneralSetting.SettingValue != null)
                    ViewBag.IsViewMarketPlaceListing = Convert.ToBoolean(generalSettingResult.GeneralSetting.SettingValue);

            var inventoryid = model.Inventory.InventoryId;
            Inventory objparam = InventoryApiUtil.GetOnlyInventoryRecord(CurrentCompanyId, inventoryid).Result.Inventory;

            var objparamMarket = InventoryApiUtil.GetMarketPlaceListingRecord(CurrentCompanyId, objparam.SKU).Result;
            model.MarketPlaceListings = objparamMarket.MarketPlaceListing;

            if (ModelState.IsValid)
            {
                model.Inventory.InventoryImages = new List<InventoryImage>();
                if (model.InventoryImages != null)
                {
                    foreach (var inventoryImages in model.InventoryImages)
                    {
                        if (inventoryImages.UpFile != null)
                        {
                            var InputFileName = Path.GetFileName(inventoryImages.ImageName);
                            var FilePath = "~/Uploads/" + CurrentCompanyId + "/InventoryImages/";
                            var DirectoryPath = Server.MapPath(FilePath);
                            if (!Directory.Exists(DirectoryPath)) //If No any such directory then creates the new one
                            {
                                Directory.CreateDirectory(DirectoryPath);
                            }
                            var ServerSavePath = Path.Combine(DirectoryPath + InputFileName);
                            //Save file to server folder  
                            inventoryImages.UpFile.SaveAs(ServerSavePath);
                            FilePath = FilePath + InputFileName;
                            //assigning file uploaded to InventoryImage Collection  
                            if (CurrentBaseCompanyId == CurrentCompanyId)
                            {
                                model.Inventory.InventoryImages.Add(new InventoryImage { InventoryImageId = inventoryImages.InventoryImageId, InventoryId = inventoryImages.InventoryId, ImageName = InputFileName, ImagePath = FilePath, Status = "A", UpdatedByUserId = CurrentUserId });
                            }
                            else
                            {
                                model.Inventory.InventoryImages.Add(new InventoryImage { InventoryImageId = inventoryImages.InventoryImageId, InventoryId = inventoryImages.InventoryId, ImageName = InputFileName, ImagePath = FilePath, Status = "A", UpdatedByUserId = SystemUserId });
                            }
                        }
                        else
                        {
                            if (CurrentBaseCompanyId == CurrentCompanyId)
                            {
                                model.Inventory.InventoryImages.Add(new InventoryImage { InventoryImageId = inventoryImages.InventoryImageId, InventoryId = inventoryImages.InventoryId, ImageName = inventoryImages.ImageName, ImagePath = inventoryImages.ImagePath, Status = "A", UpdatedByUserId = CurrentUserId });
                            }
                            else
                            {
                                model.Inventory.InventoryImages.Add(new InventoryImage { InventoryImageId = inventoryImages.InventoryImageId, InventoryId = inventoryImages.InventoryId, ImageName = inventoryImages.ImageName, ImagePath = inventoryImages.ImagePath, Status = "A", UpdatedByUserId = SystemUserId });
                            }
                        }
                    }
                }
                foreach (HttpPostedFileBase file in model.files)
                {
                    //Checking file is available to save.  
                    if (file != null)
                    {
                        var InputFileName = Path.GetFileName(file.FileName);
                        var FilePath = "~/Uploads/" + CurrentCompanyId + "/InventoryImages/";
                        var DirectoryPath = Server.MapPath(FilePath);
                        if (!Directory.Exists(DirectoryPath)) //If No any such directory then creates the new one
                        {
                            Directory.CreateDirectory(DirectoryPath);
                        }
                        var ServerSavePath = Path.Combine(DirectoryPath + InputFileName);
                        //Save file to server folder  
                        file.SaveAs(ServerSavePath);
                        FilePath = FilePath + InputFileName;
                        //assigning file uploaded to InventoryImage Collection  
                        if (CurrentBaseCompanyId == CurrentCompanyId)
                        {
                            model.Inventory.InventoryImages.Add(new InventoryImage { InventoryId = model.Inventory.InventoryId, ImageName = InputFileName, ImagePath = FilePath, Status = "A", CreatedByUserId = CurrentUserId, UpdatedByUserId = CurrentUserId });
                        }
                        else
                        {
                            model.Inventory.InventoryImages.Add(new InventoryImage { InventoryId = model.Inventory.InventoryId, ImageName = InputFileName, ImagePath = FilePath, Status = "A", CreatedByUserId = SystemUserId, UpdatedByUserId = SystemUserId });
                        }

                    }
                }

                model.Inventory.WarehouseInventories = new List<WarehouseInventory>();
                if (model.WarehouseInventories != null)
                {
                    foreach (var WarehouseInventory in model.WarehouseInventories)
                    {
                        WarehouseInventory.Warehouse = null;
                        if (CurrentBaseCompanyId == CurrentCompanyId)
                        {
                            WarehouseInventory.CreatedByUserId = CurrentUserId;
                            WarehouseInventory.UpdatedByUserId = CurrentUserId;
                        }
                        else
                        {
                            WarehouseInventory.CreatedByUserId = SystemUserId;
                            WarehouseInventory.UpdatedByUserId = SystemUserId;
                        }
                        model.Inventory.WarehouseInventories.Add(WarehouseInventory);
                    }
                }
                model.Inventory.CustomFieldInventories = new List<CustomFieldInventory>();
                if (model.CustomFieldInventories != null)
                {
                    foreach (var CustomFieldInventory in model.CustomFieldInventories)
                    {
                        CustomFieldInventory.CustomFields = null;
                        if (CurrentBaseCompanyId == CurrentCompanyId)
                        {
                            CustomFieldInventory.CreatedByUserId = CurrentUserId;
                            CustomFieldInventory.UpdatedByUserId = CurrentUserId;
                        }
                        else
                        {
                            CustomFieldInventory.CreatedByUserId = SystemUserId;
                            CustomFieldInventory.UpdatedByUserId = SystemUserId;
                        }
                        model.Inventory.CustomFieldInventories.Add(CustomFieldInventory);
                    }
                }
                model.Inventory.InventoryAliases = new List<InventoryAlias>();
                if (model.Alias != null)
                {
                    foreach (var alias in model.Alias)
                    {
                        if (!string.IsNullOrEmpty(alias.SKU))
                        {
                            if (CurrentBaseCompanyId == CurrentCompanyId)
                            {
                                alias.CreatedByUserId = CurrentUserId;
                                alias.UpdatedByUserId = CurrentUserId;
                            }
                            else
                            {
                                alias.CreatedByUserId = SystemUserId;
                                alias.UpdatedByUserId = SystemUserId;
                            }
                            model.Inventory.InventoryAliases.Add(alias);
                        }
                    }
                }
                model.Inventory.InventoryKits = new List<InventoryKit>();
                if (model.Kits != null)
                {
                    foreach (var kit in model.Kits)
                    {
                        if (!string.IsNullOrEmpty(kit.SKU))
                        {
                            if (CurrentBaseCompanyId == CurrentCompanyId)
                            {
                                kit.CreatedByUserId = CurrentUserId;
                                kit.UpdatedByUserId = CurrentUserId;
                            }
                            else
                            {
                                kit.CreatedByUserId = SystemUserId;
                                kit.UpdatedByUserId = SystemUserId;
                            }
                            model.Inventory.InventoryKits.Add(kit);
                        }
                    }
                }
                model.Inventory.ProductMarkers = new List<ProductMarker>();
                if (model.ProductMarkers != null)
                {
                    foreach (var marker in model.ProductMarkers)
                    {

                        if (CurrentBaseCompanyId == CurrentCompanyId)
                        {
                            marker.CreatedByUserId = CurrentUserId;
                            marker.UpdatedByUserId = CurrentUserId;
                        }
                        else
                        {
                            marker.CreatedByUserId = SystemUserId;
                            marker.UpdatedByUserId = SystemUserId;
                        }
                        model.Inventory.ProductMarkers.Add(marker);
                    }
                }
                model.Inventory.InventoryVariations = new List<InventoryVariation>();
                if (model.InventoryVariations != null)
                {
                    foreach (var variation in model.InventoryVariations)
                    {
                        if (CurrentBaseCompanyId == CurrentCompanyId)
                        {
                            variation.CreatedByUserId = CurrentUserId;
                            variation.UpdatedByUserId = CurrentUserId;
                        }
                        else
                        {
                            variation.CreatedByUserId = SystemUserId;
                            variation.UpdatedByUserId = SystemUserId;
                        }

                        foreach (var specification in variation.VariationSpecifications.ToList())
                        {
                            if (CurrentBaseCompanyId == CurrentCompanyId)
                            {
                                specification.CreatedByUserId = CurrentUserId;
                                specification.UpdatedByUserId = CurrentUserId;
                            }
                            else
                            {
                                specification.CreatedByUserId = SystemUserId;
                                specification.UpdatedByUserId = SystemUserId;
                            }
                        }

                        model.Inventory.InventoryVariations.Add(variation);
                    }
                }

                model.Inventory.OtherCurrencyPrices = new List<InventoryOtherCurrencyPrice>();
                if (model.OtherCurrencyPrices != null)
                {
                    foreach (var currencyPrice in model.OtherCurrencyPrices)
                    {

                        if (CurrentBaseCompanyId == CurrentCompanyId)
                        {
                            currencyPrice.CreatedByUserId = CurrentUserId;
                            currencyPrice.UpdatedByUserId = CurrentUserId;
                        }
                        else
                        {
                            currencyPrice.CreatedByUserId = SystemUserId;
                            currencyPrice.UpdatedByUserId = SystemUserId;
                        }
                        model.Inventory.OtherCurrencyPrices.Add(currencyPrice);
                    }
                }
                model.Inventory.InventoryBuyers = new List<InventoryBuyer>();
                if (model.InventoryBuyers != null)
                {
                    foreach (var inventoryBuyer in model.InventoryBuyers)
                    {
                        inventoryBuyer.Buyer = null;
                        if (CurrentBaseCompanyId == CurrentCompanyId)
                        {
                            inventoryBuyer.CreatedByUserId = CurrentUserId;
                            inventoryBuyer.UpdatedByUserId = CurrentUserId;
                        }
                        else
                        {
                            inventoryBuyer.CreatedByUserId = SystemUserId;
                            inventoryBuyer.UpdatedByUserId = SystemUserId;
                        }
                        model.Inventory.InventoryBuyers.Add(inventoryBuyer);
                    }
                }

                WebApiResponse webApiResponse = InventoryApiUtil.UpdateInventory(CurrentCompanyId, model.Inventory).Result;
                if (webApiResponse.IsSuccessful)
                {
                    ViewBag.Message = ShowMessage(MessageType.success, $"Product {model.Inventory.SKU} updated successfully.");
                    return RedirectToAction("Index");
                }
                else
                {
                    ViewBag.Message = ShowMessage(MessageType.danger, webApiResponse.Message);
                    //Get Inventories
                    model.Inventories = new List<Inventory>();
                    model.Inventories = GetInventories();
                    //Get Users
                    model.Users = new List<CommerceBitUser>();
                    model.Users = GetUsers();
                    //Get KitTypes
                    model.KitTypes = GetKitTypes();
                    //Get Warehouses
                    model.Warehouses = GetWarehouseLists();
                    //Get Warehouses
                    model.WarehouseInventories = GetWarehouseInventories(model.Inventory.InventoryId);
                    //Get Warehouses Transfer
                    model.WarehouseTransfers = new List<WarehouseTransfer>();
                    model.WarehouseTransfers = GetWarehouseTransfers(model.Inventory.InventoryId);
                    model.BrandList = new List<SelectListItem>();
                    model.ManufactureList = new List<SelectListItem>();
                    model.BrandList = GetBrandList().Select(p => new SelectListItem { Text = p.Name, Value = p.Name }).ToList();
                    model.ManufactureList = GetManufacturerList().Select(p => new SelectListItem { Text = p.Name, Value = p.Name }).ToList();
                    return View(model);
                }
            }
            //Get Inventories
            model.Inventories = new List<Inventory>();
            model.Inventories = GetInventories();
            //Get Users
            model.Users = new List<CommerceBitUser>();
            model.Users = GetUsers();
            //Get KitTypes
            model.KitTypes = GetKitTypes();
            //Get Warehouses
            model.Warehouses = GetWarehouseLists();
            //Get Warehouses
            model.WarehouseInventories = GetWarehouseInventories(model.Inventory.InventoryId);
            //Get Warehouses Transfer
            model.WarehouseTransfers = new List<WarehouseTransfer>();
            model.WarehouseTransfers = GetWarehouseTransfers(model.Inventory.InventoryId);
            model.BrandList = new List<SelectListItem>();
            model.ManufactureList = new List<SelectListItem>();
            model.BrandList = GetBrandList().Select(p => new SelectListItem { Text = p.Name, Value = p.Name }).ToList();
            model.ManufactureList = GetManufacturerList().Select(p => new SelectListItem { Text = p.Name, Value = p.Name }).ToList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(InventoryViewModel model)
        {
            dbPageId = 34;
            if (model.Inventory.IsOZorLBS)
            {
                model.Inventory.WeightOZ = model.Inventory.Weight;
                model.Inventory.WeightLBS = 0;
            }
            else
            {
                model.Inventory.WeightLBS = model.Inventory.Weight;
                model.Inventory.WeightOZ = 0;
            }

            if (CurrentBaseCompanyId == CurrentCompanyId)
            {
                model.Inventory.CreatedByUserId = CurrentUserId;
                model.Inventory.UpdatedByUserId = CurrentUserId;
            }
            else
            {
                model.Inventory.CreatedByUserId = SystemUserId;
                model.Inventory.UpdatedByUserId = SystemUserId;
            }

            if (ModelState.IsValid)
            {
                model.Inventory.InventoryImages = new List<InventoryImage>();
                if (model.InventoryImages != null)
                {
                    foreach (var inventoryImages in model.InventoryImages)
                    {
                        if (inventoryImages.UpFile != null)
                        {
                            var InputFileName = Path.GetFileName(inventoryImages.ImageName);
                            var FilePath = "~/Uploads/" + CurrentCompanyId + "/InventoryImages/";
                            var DirectoryPath = Server.MapPath(FilePath);
                            if (!Directory.Exists(DirectoryPath)) //If No any such directory then creates the new one
                            {
                                Directory.CreateDirectory(DirectoryPath);
                            }
                            var ServerSavePath = Path.Combine(DirectoryPath + InputFileName);
                            //Save file to server folder  
                            inventoryImages.UpFile.SaveAs(ServerSavePath);
                            FilePath = FilePath + InputFileName;
                            //assigning file uploaded to InventoryImage Collection  
                            if (CurrentBaseCompanyId == CurrentCompanyId)
                            {
                                model.Inventory.InventoryImages.Add(new InventoryImage { InventoryImageId = inventoryImages.InventoryImageId, InventoryId = inventoryImages.InventoryId, ImageName = InputFileName, ImagePath = FilePath, Status = "A", UpdatedByUserId = CurrentUserId });
                            }
                            else
                            {
                                model.Inventory.InventoryImages.Add(new InventoryImage { InventoryImageId = inventoryImages.InventoryImageId, InventoryId = inventoryImages.InventoryId, ImageName = InputFileName, ImagePath = FilePath, Status = "A", UpdatedByUserId = SystemUserId });
                            }
                        }
                        else
                        {
                            if (CurrentBaseCompanyId == CurrentCompanyId)
                            {
                                model.Inventory.InventoryImages.Add(new InventoryImage { InventoryImageId = inventoryImages.InventoryImageId, InventoryId = inventoryImages.InventoryId, ImageName = inventoryImages.ImageName, ImagePath = inventoryImages.ImagePath, Status = "A", UpdatedByUserId = CurrentUserId });
                            }
                            else
                            {
                                model.Inventory.InventoryImages.Add(new InventoryImage { InventoryImageId = inventoryImages.InventoryImageId, InventoryId = inventoryImages.InventoryId, ImageName = inventoryImages.ImageName, ImagePath = inventoryImages.ImagePath, Status = "A", UpdatedByUserId = SystemUserId });
                            }
                        }
                    }
                }
                foreach (HttpPostedFileBase file in model.files)
                {
                    //Checking file is available to save.  
                    if (file != null)
                    {
                        var InputFileName = Path.GetFileName(file.FileName);
                        var FilePath = "~/Uploads/" + CurrentCompanyId + "/InventoryImages/";
                        var DirectoryPath = Server.MapPath(FilePath);
                        if (!Directory.Exists(DirectoryPath)) //If No any such directory then creates the new one
                        {
                            Directory.CreateDirectory(DirectoryPath);
                        }
                        var ServerSavePath = Path.Combine(DirectoryPath + InputFileName);
                        //Save file to server folder  
                        file.SaveAs(ServerSavePath);
                        FilePath = FilePath + InputFileName;
                        //assigning file uploaded to InventoryImage Collection  
                        if (CurrentBaseCompanyId == CurrentCompanyId)
                        {
                            model.Inventory.InventoryImages.Add(new InventoryImage { InventoryId = model.Inventory.InventoryId, ImageName = InputFileName, ImagePath = FilePath, Status = "A", CreatedByUserId = CurrentUserId, UpdatedByUserId = CurrentUserId });
                        }
                        else
                        {
                            model.Inventory.InventoryImages.Add(new InventoryImage { InventoryId = model.Inventory.InventoryId, ImageName = InputFileName, ImagePath = FilePath, Status = "A", CreatedByUserId = SystemUserId, UpdatedByUserId = SystemUserId });
                        }

                    }
                }

                model.Inventory.WarehouseInventories = new List<WarehouseInventory>();
                if (model.WarehouseInventories != null)
                {
                    foreach (var WarehouseInventory in model.WarehouseInventories)
                    {
                        WarehouseInventory.Warehouse = null;
                        if (CurrentBaseCompanyId == CurrentCompanyId)
                        {
                            WarehouseInventory.CreatedByUserId = CurrentUserId;
                            WarehouseInventory.UpdatedByUserId = CurrentUserId;
                        }
                        else
                        {
                            WarehouseInventory.CreatedByUserId = SystemUserId;
                            WarehouseInventory.UpdatedByUserId = SystemUserId;
                        }
                        model.Inventory.WarehouseInventories.Add(WarehouseInventory);
                    }
                }
                model.Inventory.CustomFieldInventories = new List<CustomFieldInventory>();
                if (model.CustomFieldInventories != null)
                {
                    foreach (var CustomFieldInventory in model.CustomFieldInventories)
                    {
                        CustomFieldInventory.CustomFields = null;
                        if (CurrentBaseCompanyId == CurrentCompanyId)
                        {
                            CustomFieldInventory.CreatedByUserId = CurrentUserId;
                            CustomFieldInventory.UpdatedByUserId = CurrentUserId;
                        }
                        else
                        {
                            CustomFieldInventory.CreatedByUserId = SystemUserId;
                            CustomFieldInventory.UpdatedByUserId = SystemUserId;
                        }
                        model.Inventory.CustomFieldInventories.Add(CustomFieldInventory);
                    }
                }
                model.Inventory.InventoryAliases = new List<InventoryAlias>();
                if (model.Alias != null)
                {
                    foreach (var alias in model.Alias)
                    {
                        if (!string.IsNullOrEmpty(alias.SKU))
                        {
                            if (CurrentBaseCompanyId == CurrentCompanyId)
                            {
                                alias.CreatedByUserId = CurrentUserId;
                                alias.UpdatedByUserId = CurrentUserId;
                            }
                            else
                            {
                                alias.CreatedByUserId = SystemUserId;
                                alias.UpdatedByUserId = SystemUserId;
                            }
                            model.Inventory.InventoryAliases.Add(alias);
                        }
                    }
                }
                model.Inventory.InventoryKits = new List<InventoryKit>();
                if (model.Kits != null)
                {
                    foreach (var kit in model.Kits)
                    {
                        if (!string.IsNullOrEmpty(kit.SKU))
                        {
                            if (CurrentBaseCompanyId == CurrentCompanyId)
                            {
                                kit.CreatedByUserId = CurrentUserId;
                                kit.UpdatedByUserId = CurrentUserId;
                            }
                            else
                            {
                                kit.CreatedByUserId = SystemUserId;
                                kit.UpdatedByUserId = SystemUserId;
                            }
                            model.Inventory.InventoryKits.Add(kit);
                        }
                    }
                }
                model.Inventory.ProductMarkers = new List<ProductMarker>();
                if (model.ProductMarkers != null)
                {
                    foreach (var marker in model.ProductMarkers)
                    {

                        if (CurrentBaseCompanyId == CurrentCompanyId)
                        {
                            marker.CreatedByUserId = CurrentUserId;
                            marker.UpdatedByUserId = CurrentUserId;
                        }
                        else
                        {
                            marker.CreatedByUserId = SystemUserId;
                            marker.UpdatedByUserId = SystemUserId;
                        }
                        model.Inventory.ProductMarkers.Add(marker);
                    }
                }
                model.Inventory.InventoryVariations = new List<InventoryVariation>();
                if (model.InventoryVariations != null)
                {
                    foreach (var variation in model.InventoryVariations)
                    {
                        if (CurrentBaseCompanyId == CurrentCompanyId)
                        {
                            variation.CreatedByUserId = CurrentUserId;
                            variation.UpdatedByUserId = CurrentUserId;
                        }
                        else
                        {
                            variation.CreatedByUserId = SystemUserId;
                            variation.UpdatedByUserId = SystemUserId;
                        }

                        foreach (var specification in variation.VariationSpecifications.ToList())
                        {
                            if (CurrentBaseCompanyId == CurrentCompanyId)
                            {
                                specification.CreatedByUserId = CurrentUserId;
                                specification.UpdatedByUserId = CurrentUserId;
                            }
                            else
                            {
                                specification.CreatedByUserId = SystemUserId;
                                specification.UpdatedByUserId = SystemUserId;
                            }
                        }

                        model.Inventory.InventoryVariations.Add(variation);
                    }
                }

                model.Inventory.OtherCurrencyPrices = new List<InventoryOtherCurrencyPrice>();
                if (model.OtherCurrencyPrices != null)
                {
                    foreach (var currencyPrice in model.OtherCurrencyPrices)
                    {

                        if (CurrentBaseCompanyId == CurrentCompanyId)
                        {
                            currencyPrice.CreatedByUserId = CurrentUserId;
                            currencyPrice.UpdatedByUserId = CurrentUserId;
                        }
                        else
                        {
                            currencyPrice.CreatedByUserId = SystemUserId;
                            currencyPrice.UpdatedByUserId = SystemUserId;
                        }
                        model.Inventory.OtherCurrencyPrices.Add(currencyPrice);
                    }
                }

                model.Inventory.InventoryBuyers = new List<InventoryBuyer>();
                if (model.InventoryBuyers != null)
                {
                    foreach (var inventoryBuyer in model.InventoryBuyers)
                    {
                        inventoryBuyer.Buyer = null;
                        if (CurrentBaseCompanyId == CurrentCompanyId)
                        {
                            inventoryBuyer.CreatedByUserId = CurrentUserId;
                            inventoryBuyer.UpdatedByUserId = CurrentUserId;
                        }
                        else
                        {
                            inventoryBuyer.CreatedByUserId = SystemUserId;
                            inventoryBuyer.UpdatedByUserId = SystemUserId;
                        }
                        model.Inventory.InventoryBuyers.Add(inventoryBuyer);
                    }
                }

                WebApiResponse webApiResponse = InventoryApiUtil.UpdateInventory(CurrentCompanyId, model.Inventory).Result;
                if (webApiResponse.IsSuccessful)
                {
                    //ViewBag.Message = ShowMessage(MessageType.success, $"Product {model.Inventory.SKU} updated successfully.");
                    //return View(model);
                    TempData["displayalert"] = true;
                    TempData["alert"] = $"Product {model.Inventory.SKU} updated successfully.";
                    return RedirectToAction("Edit", "Inventory", new { productid = model.Inventory.InventoryId.ToString() });
                }
                else
                {
                    ViewBag.Message = ShowMessage(MessageType.danger, webApiResponse.Message);
                    //Get Inventories
                    model.Inventories = new List<Inventory>();
                    model.Inventories = GetInventories();
                    //Get Users
                    model.Users = new List<CommerceBitUser>();
                    model.Users = GetUsers();
                    //Get KitTypes
                    model.KitTypes = GetKitTypes();
                    //Get Warehouses
                    model.Warehouses = GetWarehouseLists();
                    //Get Warehouses
                    model.WarehouseInventories = GetWarehouseInventories(model.Inventory.InventoryId);
                    //Get Warehouses Transfer
                    model.WarehouseTransfers = new List<WarehouseTransfer>();
                    model.WarehouseTransfers = GetWarehouseTransfers(model.Inventory.InventoryId);
                    model.BrandList = new List<SelectListItem>();
                    model.ManufactureList = new List<SelectListItem>();
                    model.BrandList = GetBrandList().Select(p => new SelectListItem { Text = p.Name, Value = p.Name }).ToList();
                    model.ManufactureList = GetManufacturerList().Select(p => new SelectListItem { Text = p.Name, Value = p.Name }).ToList();

                    TempData["displayalert"] = false;
                    TempData["displaymodel"] = model;
                    TempData["alert"] = $"Product {model.Inventory.SKU} not updated successfully. {webApiResponse.Message}";
                    return RedirectToAction("Edit", "Inventory", new { productid = model.Inventory.InventoryId.ToString() });
                }
            }
            var errorMessage = "";
            foreach (ModelState modelState in ViewData.ModelState.Values)
            {
                foreach (ModelError error in modelState.Errors)
                {
                    errorMessage = errorMessage + error.ErrorMessage.ToString();
                }
            }

            //Get Inventories
            model.Inventories = new List<Inventory>();
            model.Inventories = GetInventories();
            //Get Users
            model.Users = new List<CommerceBitUser>();
            model.Users = GetUsers();
            //Get KitTypes
            model.KitTypes = GetKitTypes();
            //Get Warehouses
            model.Warehouses = GetWarehouseLists();
            //Get Warehouses
            model.WarehouseInventories = GetWarehouseInventories(model.Inventory.InventoryId);
            //Get Warehouses Transfer
            model.WarehouseTransfers = new List<WarehouseTransfer>();
            model.WarehouseTransfers = GetWarehouseTransfers(model.Inventory.InventoryId);
            model.BrandList = new List<SelectListItem>();
            model.ManufactureList = new List<SelectListItem>();
            model.BrandList = GetBrandList().Select(p => new SelectListItem { Text = p.Name, Value = p.Name }).ToList();
            model.ManufactureList = GetManufacturerList().Select(p => new SelectListItem { Text = p.Name, Value = p.Name }).ToList();

            TempData["displayalert"] = false;
            TempData["displaymodel"] = model;
            TempData["alert"] = errorMessage;
            return RedirectToAction("Edit", "Inventory", new { productid = model.Inventory.InventoryId.ToString() });
        }

        public ActionResult Duplicate(string productid)
        {
            dbPageId = 34;
            var model = new Duplicate();
            int id = Convert.ToInt32(productid);
            var inventoryApiResponse = InventoryApiUtil.GetInventoryRecord(CurrentCompanyId, id).Result;
            if (inventoryApiResponse.Inventory != null)
            {
                model.FromInventoryId = inventoryApiResponse.Inventory.InventoryId;
                model.FromSKU = inventoryApiResponse.Inventory.SKU;
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Duplicate(Duplicate model)
        {
            dbPageId = 34;
            if (!string.IsNullOrEmpty(model.DuplicateSKU))
            {
                var inventoryApiResponse = InventoryApiUtil.AddDuplicateInventory(CurrentCompanyId, model).Result;
                if (inventoryApiResponse.IsSuccessful)
                    ViewBag.Message = ShowMessage(MessageType.success, $"New SKU {model.DuplicateSKU} created successfully.");
                else
                    ViewBag.Message = ShowMessage(MessageType.danger, "Somthing went wrong! " + inventoryApiResponse.Message);
            }
            else
                ViewBag.Message = ShowMessage(MessageType.danger, "Please, Enter new SKU.");
            return View(model);
        }

        // /Inventory/InventoryCount
        public ActionResult InventoryCount()
        {
            dbPageId = 61;
            var model = new InventoryCountViewModel()
            {
                InventoryCounts = new List<InventoryCount>(),
                InventoryCountFilter = new InventoryCountFilter()
                {
                    CountedByList = new List<SelectListItem>()
                }
            };
            var response = InventoryCountApiUtil.GetAllInventoryCountRecords(CurrentCompanyId).Result;
            if (response.InventoryCounts != null)
                model.InventoryCounts = response.InventoryCounts;

            model.InventoryCountFilter.CountedByList = GetUsers().Select(p => new SelectListItem { Text = p.FullName, Value = p.CommerceBitUserId.ToString() }).ToList();

            return View(model);
        }

        [HttpGet]
        public ActionResult MoreFilterInventoryCountListPartialView(InventoryCountFilter Filterdata)
        {
            dbPageId = 34;
            var pagerSorter = new PagerSorterUtil()
            {
                skip = 0,
                pageSize = 15,
                sortColumn = "InventoryCountId",
                sortColumnDir = "desc",
                //SearchField = Filterdata.CustomType,
                //SearchValue = Filterdata.CustomValue
            };
            var inventoryCountApiResponse = InventoryCountApiUtil.GetFilteredInventoryCountPageListRecord(CurrentCompanyId, Filterdata, pagerSorter).Result;
            if (inventoryCountApiResponse.IsSuccessful)
                if (inventoryCountApiResponse.InventoryCountPagedList != null)
                    return PartialView("_InventoryCountList", inventoryCountApiResponse.InventoryCountPagedList.data);

            return PartialView("_InventoryCountList", new List<InventoryCount>());
        }

        // /Inventory/PerformNewCount
        public ActionResult PerformNewCount()
        {
            dbPageId = 61;
            var model = new InventoryCountViewModel()
            {
                InventoryCount = new InventoryCount(),
                InventorySKUUPCCounts = new List<InventorySKUUPCCount>(),
                Warehouses = new List<Warehouse>()
            };
            model.InventoryCount.CountId = GetNextCountId();
            model.Warehouses = GetWarehouseLists().Where(w => w.WarehouseType == "Local Warehouse").ToList();
            var userResponse = UserApiUtil.GetUserById(CurrentCompanyId, CurrentUserId).Result;
            if (userResponse.CommerceBitUser != null)
                ViewBag.FullName = userResponse.CommerceBitUser.FullName;
            return View(model);
        }

        private int GetNextCountId()
        {
            var response = InventoryCountApiUtil.GetNextCountId(CurrentCompanyId).Result;
            if (response.Id > 0)
                return response.Id + 1;
            else
                return 100;
        }

        // /Inventory/PerformNewCount
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PerformNewCount(InventoryCountViewModel model)
        {
            dbPageId = 61;
            if (CurrentBaseCompanyId == CurrentCompanyId)
            {
                model.InventoryCount.CreatedByUserId = CurrentUserId;
                model.InventoryCount.UpdatedByUserId = CurrentUserId;
            }
            else
            {
                model.InventoryCount.CreatedByUserId = SystemUserId;
                model.InventoryCount.UpdatedByUserId = SystemUserId;
            }
            if (ModelState.IsValid)
            {
                if (model.InventorySKUUPCCounts != null)
                {
                    model.InventoryCount.InventorySKUUPCCounts = new List<InventorySKUUPCCount>();
                    foreach (var item in model.InventorySKUUPCCounts)
                    {
                        if (CurrentBaseCompanyId == CurrentCompanyId)
                        {
                            item.CreatedByUserId = CurrentUserId;
                            item.UpdatedByUserId = CurrentUserId;
                        }
                        else
                        {
                            item.CreatedByUserId = SystemUserId;
                            item.UpdatedByUserId = SystemUserId;
                        }
                        model.InventoryCount.InventorySKUUPCCounts.Add(item);
                    }
                }

                var response = InventoryCountApiUtil.AddInventoryCount(CurrentCompanyId, model.InventoryCount).Result;
                if (response.IsSuccessful)
                {
                    if (model.InventorySKUUPCCounts != null)
                    {
                        foreach (var item in model.InventorySKUUPCCounts)
                        {
                            item.CountApplied = "Committed";
                        }
                    }
                    ViewBag.Message = ShowMessage(MessageType.success, "Inventory count updated successfully.");
                }
                else
                    ViewBag.Message = ShowMessage(MessageType.danger, $"Someting went to wrong! {response.Message}");
            }
            var userResponse = UserApiUtil.GetUserById(CurrentCompanyId, CurrentUserId).Result;
            if (userResponse.CommerceBitUser != null)
                ViewBag.FullName = userResponse.CommerceBitUser.FullName;
            model.Warehouses = GetWarehouseLists().Where(w => w.WarehouseType == "Local Warehouse").ToList();
            return View(model);
        }

        // /Inventory/PerformEditCount
        public ActionResult PerformEditCount(string countid)
        {
            dbPageId = 61;
            var CountId = Convert.ToInt32(countid);
            var model = new InventoryCountViewModel()
            {
                InventoryCount = new InventoryCount() { CountId = GetNextCountId() },
                InventorySKUUPCCounts = new List<InventorySKUUPCCount>(),
                Warehouses = new List<Warehouse>()
            };

            if (TempData["displayalert"] != null)
            {
                if (Convert.ToBoolean(TempData["displayalert"]) == true)
                    ViewBag.Message = ShowMessage(MessageType.success, TempData["alert"].ToString());
                else if (Convert.ToBoolean(TempData["displayalert"]) == false)
                    ViewBag.Message = ShowMessage(MessageType.danger, TempData["alert"].ToString());
            }

            var response = InventoryCountApiUtil.GetInventoryCountCountId(CurrentCompanyId, CountId).Result;
            if (response.InventoryCount != null)
                model.InventoryCount = response.InventoryCount;
            if (model.InventoryCount != null)
                if (model.InventoryCount.InventorySKUUPCCounts != null)
                    model.InventorySKUUPCCounts.AddRange(model.InventoryCount.InventorySKUUPCCounts);

            var userResponse = UserApiUtil.GetUserById(CurrentCompanyId, CurrentUserId).Result;
            if (userResponse.CommerceBitUser != null)
                ViewBag.FullName = userResponse.CommerceBitUser.FullName;
            model.Warehouses = GetWarehouseLists().Where(w => w.WarehouseType == "Local Warehouse").ToList();

            return View(model);
        }

        // /Inventory/PerformEditCount
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PerformEditCount(InventoryCountViewModel model)
        {
            dbPageId = 61;
            if (CurrentBaseCompanyId == CurrentCompanyId)
            {
                model.InventoryCount.CreatedByUserId = CurrentUserId;
                model.InventoryCount.UpdatedByUserId = CurrentUserId;
            }
            else
            {
                model.InventoryCount.CreatedByUserId = SystemUserId;
                model.InventoryCount.UpdatedByUserId = SystemUserId;
            }
            if (ModelState.IsValid)
            {
                if (model.InventorySKUUPCCounts != null)
                {
                    model.InventoryCount.InventorySKUUPCCounts = new List<InventorySKUUPCCount>();
                    foreach (var item in model.InventorySKUUPCCounts)
                    {
                        if (CurrentBaseCompanyId == CurrentCompanyId)
                        {
                            item.CreatedByUserId = CurrentUserId;
                            item.UpdatedByUserId = CurrentUserId;
                        }
                        else
                        {
                            item.CreatedByUserId = SystemUserId;
                            item.UpdatedByUserId = SystemUserId;
                        }
                        model.InventoryCount.InventorySKUUPCCounts.Add(item);
                    }
                }

                var response = InventoryCountApiUtil.UpdateInventoryCount(CurrentCompanyId, model.InventoryCount).Result;
                if (response.IsSuccessful)
                {
                    if (model.InventorySKUUPCCounts != null)
                    {
                        foreach (var item in model.InventorySKUUPCCounts)
                        {
                            item.CountApplied = "Committed";
                        }
                    }
                    TempData["displayalert"] = true;
                    TempData["alert"] = $"Inventory Count:{model.InventoryCount.CountId} Updated successfully.";
                    return RedirectToAction("PerformEditCount", "Inventory", new { @countid = model.InventoryCount.CountId });
                }
                else
                {
                    model.Warehouses = GetWarehouseLists().Where(w => w.WarehouseType == "Local Warehouse").ToList();
                    var updateuserResponse = UserApiUtil.GetUserById(CurrentCompanyId, CurrentUserId).Result;
                    if (updateuserResponse.CommerceBitUser != null)
                        ViewBag.FullName = updateuserResponse.CommerceBitUser.FullName;

                    TempData["displayalert"] = false;
                    TempData["displaymodel"] = model;
                    TempData["alert"] = $"Inventory Count:{model.InventoryCount.CountId} DID NOT Updated changes. {response.Message}";
                    return RedirectToAction("PerformEditCount", "Inventory", new { @countid = model.InventoryCount.CountId });
                }
            }

            var errorMessage = "";
            foreach (ModelState modelState in ViewData.ModelState.Values)
            {
                foreach (ModelError error in modelState.Errors)
                {
                    errorMessage = errorMessage + error.ErrorMessage.ToString();
                }
            }

            model.Warehouses = GetWarehouseLists().Where(w => w.WarehouseType == "Local Warehouse").ToList();
            var userResponse = UserApiUtil.GetUserById(CurrentCompanyId, CurrentUserId).Result;
            if (userResponse.CommerceBitUser != null)
                ViewBag.FullName = userResponse.CommerceBitUser.FullName;

            TempData["displayalert"] = false;
            TempData["displaymodel"] = model;
            TempData["alert"] = $"Inventory Count:{model.InventoryCount.CountId} DID NOT Updated changes. {errorMessage}";
            return RedirectToAction("PerformEditCount", "Inventory", new { @countid = model.InventoryCount.CountId });
        }

        [HttpGet]
        public ActionResult DeleteInventoryCount(string id)
        {
            dbPageId = 61;
            var objparam = InventoryCountApiUtil.DeleteInventoryCountById(CurrentCompanyId, Convert.ToInt32(id)).Result;
            if (objparam.IsSuccessful)
                return Json("ok", JsonRequestBehavior.AllowGet);
            else
                return Json("fail", JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult GetInventoryBySKU(string val)
        {
            dbPageId = 34;
            Inventory objparam = InventoryApiUtil.GetInventoryRecordBySKU(CurrentCompanyId, val).Result.Inventory;

            return Json(objparam, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public ActionResult GetInventoryBySKUandWarehouseId(string val, int wId)
        {
            dbPageId = 34;
            Inventory objparam = InventoryApiUtil.GetInventoryBySKUandWarehouseId(CurrentCompanyId, val, wId).Result.Inventory;

            return Json(objparam, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public ActionResult GetInventoryBySKUandWarehouseIdandIsUPC(string val, int wId)
        {
            dbPageId = 34;
            Inventory objparam = InventoryApiUtil.GetInventoryBySKUandWarehouseIdIsUPC(CurrentCompanyId, val, wId).Result.Inventory;

            return Json(objparam, JsonRequestBehavior.AllowGet);
        }
        private List<WarehouseInventory> GetWarehouseInventories(int inventoryId)
        {
            var warehouseInventoryApiResponse = WarehouseInventoryApiUtil.GetWarehouseInventoryWithWarehousesByinventoryId(CurrentCompanyId, inventoryId).Result;
            if (warehouseInventoryApiResponse.WarehouseInventories != null)
                return warehouseInventoryApiResponse.WarehouseInventories;
            else
                return new List<WarehouseInventory>();
        }
        private List<WarehouseTransfer> GetWarehouseTransfers(int inventoryId)
        {
            var warehouseApiResponse = WarehouseTransferApiUtil.GetWarehouseTransferRecordByInventoryId(CurrentCompanyId, inventoryId).Result;
            if (warehouseApiResponse.WarehouseTransfers != null)
                return warehouseApiResponse.WarehouseTransfers;
            else
                return new List<WarehouseTransfer>();
        }
        private List<SelectListItem> GetKitTypes()
        {
            KitTypeMasterApiResponse kitTypeMasterApiResponse = KitTypeMasterApiUtil.GetAllKitTypeMasterRecords(CurrentCompanyId).Result;
            return (from p in kitTypeMasterApiResponse.KitTypeMasters
                    select new SelectListItem
                    {
                        Text = p.KitType,
                        Value = p.KitTypeId.ToString()
                    }).ToList();
        }
        [HttpGet]
        public ActionResult GetKitTypeList()
        {
            dbPageId = 34;
            KitTypeMasterApiResponse kitTypeMasterApiResponse = KitTypeMasterApiUtil.GetAllKitTypeMasterRecords(CurrentCompanyId).Result;
            return Json(kitTypeMasterApiResponse.KitTypeMasters, JsonRequestBehavior.AllowGet);
        }

        private List<Inventory> GetInventories()
        {
            var response = InventoryApiUtil.GetAllInventoryRecords(CurrentCompanyId).Result;
            if (response.Inventories != null)
                return response.Inventories;
            else
                return new List<Inventory>();
        }
        [HttpGet]
        public ActionResult GetInventoryList()
        {
            dbPageId = 34;
            List<Inventory> objparam = GetInventories();
            return Json(objparam, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public ActionResult GetInventory(string val)
        {
            dbPageId = 34;
            int Id = Convert.ToInt32(val);
            //Inventory objparam = InventoryApiUtil.GetInventoryRecord(CurrentCompanyId, Id).Result.Inventory;
            Inventory objparam = InventoryApiUtil.GetOnlyInventoryRecord(CurrentCompanyId, Id).Result.Inventory;

            return Json(objparam, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public ActionResult GetVendorSKUDetails(string id)
        {
            dbPageId = 34;
            int Id = Convert.ToInt32(id);
            //Inventory objparam = InventoryApiUtil.GetInventoryRecord(CurrentCompanyId, Id).Result.Inventory;
            InventoryVendorSKU objparam = InventoryVendorSKUApiUtil.GetInventoryVendorSKU(CurrentCompanyId, Id).Result.InventoryVendorSKU;

            return Json(objparam, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult SaveUpdateVendorSKUDetails(InventoryVendorSKU vendorSKU)
        {
            dbPageId = 34;
            if (CurrentBaseCompanyId == CurrentCompanyId)
            {
                vendorSKU.CreatedByUserId = CurrentUserId;
                vendorSKU.UpdatedByUserId = CurrentUserId;
            }
            else
            {
                vendorSKU.CreatedByUserId = SystemUserId;
                vendorSKU.UpdatedByUserId = SystemUserId;
            }
            if (ModelState.IsValid)
            {
                //Inventory objparam = InventoryApiUtil.GetInventoryRecord(CurrentCompanyId, Id).Result.Inventory;
                InventoryVendorSKUApiResponse objparam;
                if (vendorSKU.InventoryVendorSKUId > 0)
                    objparam = InventoryVendorSKUApiUtil.UpdateInventoryVendorSKU(CurrentCompanyId, vendorSKU).Result;
                else
                    objparam = InventoryVendorSKUApiUtil.AddInventoryVendorSKU(CurrentCompanyId, vendorSKU).Result;

                if (objparam.IsSuccessful)
                    return Json(new { status = "ok", message = "" }, JsonRequestBehavior.AllowGet);
                else
                    return Json(new { status = "fail", message = objparam.Message }, JsonRequestBehavior.AllowGet);
            }
            var errorMessage = "";
            foreach (ModelState modelState in ViewData.ModelState.Values)
            {
                foreach (ModelError error in modelState.Errors)
                {
                    errorMessage = errorMessage + error.ErrorMessage.ToString();
                }
            }
            return Json(new { status = "fail", message = errorMessage }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult DeleteInventoryVendorSKUById(string val)
        {
            dbPageId = 34;
            int Id = Convert.ToInt32(val);
            var objparam = InventoryVendorSKUApiUtil.DeleteInventoryVendorSKUById(CurrentCompanyId, Id).Result;
            if (objparam.IsSuccessful)
                return Json(objparam.Id, JsonRequestBehavior.AllowGet);
            else
                return Json(0, JsonRequestBehavior.AllowGet);
        }
        private List<Order> GetOrderRecordsByInventoryId(int inventoryId, int pageIndex, int pageSize)
        {
            var orderApiResponse = OrderApiUtil.GetOrderRecordsByInventoryId(CurrentCompanyId, inventoryId, pageIndex, pageSize).Result;
            if (orderApiResponse.Orders != null)
            {
                return orderApiResponse.Orders;
            }
            return new List<Order>();
        }
        private List<PurchaseOrder> GetPurchaseOrderRecordsByInventoryId(int inventoryId, int pageIndex, int pageSize)
        {
            var pagerSorter = new PagerSorterUtil()
            {
                skip = 0,
                pageSize = pageSize,
                sortColumn = "PurchaseOrderId",
                sortColumnDir = "desc",
                SearchField = "InventoryId",
                SearchValue = inventoryId.ToString()
            };
            var orderApiResponse = PurchaseOrderApiUtil.GetPurchaseOrdersByInventoryPagedListRecords(CurrentCompanyId, pagerSorter).Result;
            if (orderApiResponse.PurchaseOrdersPagedList != null)
            {
                return orderApiResponse.PurchaseOrdersPagedList.data;
            }
            return new List<PurchaseOrder>();
        }
        [HttpGet]
        public ActionResult RemoveInventoryImage(string val)
        {
            dbPageId = 34;
            var objparam = InventoryApiUtil.DeleteInventoryImage(CurrentCompanyId, Convert.ToInt32(val)).Result;
            if (objparam.IsSuccessful)
                return Json("ok", JsonRequestBehavior.AllowGet);
            else
                return Json("fail", JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult DeleteInventoryAliasByAliasId(string val)
        {
            dbPageId = 34;
            int Id = Convert.ToInt32(val);
            var objparam = InventoryApiUtil.DeleteInventoryAliasByAliasId(CurrentCompanyId, Id).Result;
            if (objparam.IsSuccessful)
                return Json(objparam.InventoryId, JsonRequestBehavior.AllowGet);
            else
                return Json(0, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult DeleteInventoryKitByKitId(string val)
        {
            dbPageId = 34;
            int Id = Convert.ToInt32(val);
            var objparam = InventoryApiUtil.DeleteInventoryKitByKitId(CurrentCompanyId, Id).Result;
            if (objparam.IsSuccessful)
                return Json(objparam.InventoryId, JsonRequestBehavior.AllowGet);
            else
                return Json(0, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult DeleteInventoryVariationByVariationId(string val)
        {
            dbPageId = 34;
            int Id = Convert.ToInt32(val);
            var objparam = InventoryApiUtil.DeleteInventoryVariationByVariationId(CurrentCompanyId, Id).Result;
            if (objparam.IsSuccessful)
                return Json(objparam.InventoryId, JsonRequestBehavior.AllowGet);
            else
                return Json(0, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult DeleteProductMarkerById(string val)
        {
            dbPageId = 34;
            int Id = Convert.ToInt32(val);
            var objparam = InventoryApiUtil.DeleteProductMarkerById(CurrentCompanyId, Id).Result;
            if (objparam.IsSuccessful)
                return Json(objparam.InventoryId, JsonRequestBehavior.AllowGet);
            else
                return Json(0, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult DeleteOtherCurrencyPricesById(string val)
        {
            dbPageId = 34;
            int Id = Convert.ToInt32(val);
            var objparam = InventoryApiUtil.DeleteOtherCurrencyPricesById(CurrentCompanyId, Id).Result;
            if (objparam.IsSuccessful)
                return Json(objparam.InventoryId, JsonRequestBehavior.AllowGet);
            else
                return Json(0, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult DeleteInventoryBuyerById(string val)
        {
            dbPageId = 34;
            int Id = Convert.ToInt32(val);
            var objparam = InventoryApiUtil.DeleteInventoryBuyerById(CurrentCompanyId, Id).Result;
            if (objparam.IsSuccessful)
                return Json(objparam.InventoryId, JsonRequestBehavior.AllowGet);
            else
                return Json(0, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult DeleteSelectedInventory(List<int> ids)
        {
            dbPageId = 34;
            var objparam = InventoryApiUtil.DeleteSelectedInventory(CurrentCompanyId, ids).Result;
            if (objparam.IsSuccessful)
                return Json("ok", JsonRequestBehavior.AllowGet);
            else
                return Json("fail", JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult UploadFile()
        {
            dbPageId = 34;
            var InputFileName = "";
            var FilePath = "";
            var InventoryId = Request.Form["InventoryId"];
            var ImageId = Request.Form["ImageId"];
            var ImageCount = Convert.ToInt32(Request.Form["ImageCount"]);
            var file = Request.Files;

            if (file[0] != null && file[0].ContentLength > 0)
            {
                //var Id = 0;
                string[] ExtensionList = { ".ddl", ".exe" };
                var Ext = Path.GetExtension(file[0].FileName);
                if (ExtensionList.Contains(Ext))
                    return Json(null);

                ImageCount = ImageCount + 1;
                InputFileName = "Image" + ImageCount + Ext;
                System.Drawing.Image img = System.Drawing.Image.FromStream(file[0].InputStream);
                int height = img.Height;
                int width = img.Width;
                FilePath = "~/Uploads/" + CurrentCompanyId + "/InventoryImages/" + InventoryId + "/";
                var DirectoryPath = Server.MapPath(FilePath);
                if (!Directory.Exists(DirectoryPath)) //If No any such directory then creates the new one
                {
                    Directory.CreateDirectory(DirectoryPath);
                }
                var ServerSavePath = Path.Combine(DirectoryPath + InputFileName);
                //Save file to server folder  
                file[0].SaveAs(ServerSavePath);
                FilePath = FilePath + InputFileName;

                //assigning file uploaded to InventoryImage Collection  
                var model = new InventoryImage();
                model.InventoryImageId = Convert.ToInt32(ImageId);
                model.InventoryId = Convert.ToInt32(InventoryId);
                model.ImageName = InputFileName;
                model.ImagePath = FilePath;
                model.ImageHeight = height;
                model.ImageWidth = width;
                model.Status = "A";
                if (CurrentBaseCompanyId == CurrentCompanyId)
                {
                    model.CreatedByUserId = CurrentUserId;
                    model.UpdatedByUserId = CurrentUserId;
                }
                else
                {
                    model.CreatedByUserId = SystemUserId;
                    model.UpdatedByUserId = SystemUserId;
                }
                var image = InventoryApiUtil.AddUpdateInventoryImage(CurrentCompanyId, model).Result;
                if (image.IsSuccessful)
                    return Json(new { DisplayName = InputFileName, Path = FilePath });
                else
                    return Json(null);
            }
            return Json(null);
        }
        [HttpGet]
        public ActionResult InventoryListPartialView(string SearchType, string SearchValue)
        {
            dbPageId = 34;
            var pagerSorter = new PagerSorterUtil()
            {
                skip = 0,
                pageSize = 15,
                sortColumn = "InventoryId",
                sortColumnDir = "desc",
                SearchField = SearchType,
                SearchValue = SearchValue
            };

            InventoryApiResponse inventoryApiResponse = InventoryApiUtil.GetInventoryPagedListRecords(CurrentCompanyId, pagerSorter).Result;
            if (inventoryApiResponse.IsSuccessful)
                if (inventoryApiResponse.InventoryPagedList != null)
                    return PartialView("_InventoryList", inventoryApiResponse.InventoryPagedList.data);

            return PartialView("_InventoryList", new List<Inventory>());
        }
        [HttpGet]
        public ActionResult MoreFilterInventoryListPartialView(InventoryFilter Filterdata)
        {
            dbPageId = 34;
            var pagerSorter = new PagerSorterUtil()
            {
                skip = 0,
                page = Filterdata.Page,
                sortColumn = "InventoryId",
                sortColumnDir = "desc",
                pageSize = Filterdata.PageSize
                //SearchField = Filterdata.CustomType,
                //SearchValue = Filterdata.CustomValue
            };
            InventoryApiResponse inventoryApiResponse = InventoryApiUtil.GetFilteredInventoryPageListRecord(CurrentCompanyId, Filterdata, pagerSorter).Result;
            var model = inventoryApiResponse.InventoryOverView;
            model.CurrentPage = pagerSorter.page;
            var PageListUtil = new PageListUtil<Inventory>(model.TotalCount, model.CurrentPage, model.pageSize);
            PageListUtil.data = new List<Inventory>();
            PageListUtil.data = model.Inventories;
            PageListUtil.TotalValue = model.TotalValue;
            PageListUtil.CurrentPage = model.CurrentPage;
            PageListUtil.StartIndex = model.StartPageIndex;
            PageListUtil.EndIndex = model.EndPageIndex;
            //if (inventoryApiResponse.IsSuccessful)           
            return PartialView("_MoreFilterInventoryListPartialView", PageListUtil);
        }
        [HttpGet]
        public ActionResult DeleteInventory(string id)
        {
            dbPageId = 34;
            var objparam = InventoryApiUtil.DeleteInventory(CurrentCompanyId, Convert.ToInt32(id)).Result;
            if (objparam.IsSuccessful && objparam.InventoryId > 0)
                return Json("ok", JsonRequestBehavior.AllowGet);
            else
                return Json("fail", JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AddInventorySearch(string SaveName, string SavedString)
        {
            dbPageId = 34;
            var model = new InventorySearch();
            model.SearchName = SaveName;
            model.SearchString = SavedString;
            if (CurrentBaseCompanyId == CurrentCompanyId)
            {
                model.CreatedByUserId = CurrentUserId;
                model.UpdatedByUserId = CurrentUserId;
            }
            else
            {
                model.CreatedByUserId = SystemUserId;
                model.UpdatedByUserId = SystemUserId;
            }

            WebApiResponse webApiResponse = InventorySearchApiUtil.AddInventorySearch(CurrentCompanyId, model).Result;
            if (webApiResponse.IsSuccessful)
            {
                return Json("ok", JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json("fail", JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult GetInventorySearchList()
        {
            dbPageId = 34;
            List<InventorySearch> objparam = InventorySearchApiUtil.GetInventorySearchRecords(CurrentCompanyId).Result.InventorySearches;
            return Json(objparam, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult GetInventorySearchById(int SearchId)
        {
            dbPageId = 34;
            InventorySearch objparam = InventorySearchApiUtil.GetInventorySearchBySearchId(CurrentCompanyId, SearchId).Result.InventorySearch;
            return Json(objparam, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult GetInventoryAliasList(string id)
        {
            dbPageId = 34;
            var inventoryApiResponse = InventoryApiUtil.GetInventoryRecord(CurrentCompanyId, Convert.ToInt32(id)).Result;
            if (inventoryApiResponse.IsSuccessful)
                return PartialView("_AliasItemList", inventoryApiResponse.Inventory.InventoryAliases.ToList());
            else
                return PartialView("_AliasItemList", null);
        }
        [HttpGet]
        public ActionResult GetInventoryKittingList(string id)
        {
            dbPageId = 34;
            var inventoryApiResponse = InventoryApiUtil.GetInventoryRecord(CurrentCompanyId, Convert.ToInt32(id)).Result;
            if (inventoryApiResponse.IsSuccessful)
                return PartialView("_KittingItemList", inventoryApiResponse.Inventory.InventoryKits.ToList());
            else
                return PartialView("_KittingItemList", null);
        }

        [HttpGet]
        public ActionResult GetInventoryVariationList(string id)
        {
            dbPageId = 34;
            var inventoryApiResponse = InventoryApiUtil.GetInventoryRecord(CurrentCompanyId, Convert.ToInt32(id)).Result;
            if (inventoryApiResponse.IsSuccessful)
                return PartialView("_VariationItemList", inventoryApiResponse.Inventory.InventoryVariations.ToList());
            else
                return PartialView("_VariationItemList", null);
        }
        [HttpGet]
        public ActionResult GetVendorList()
        {
            dbPageId = 34;
            List<Vendor> objparam = VendorApiUtil.GetAllVendorRecords(CurrentCompanyId).Result.Vendors;
            return Json(objparam, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public ActionResult GetVariationList()
        {
            dbPageId = 34;
            List<VariationSetting> objparam = VariationSettingApiUtil.GetAllVariationSettingRecords(CurrentCompanyId).Result.VariationSettings;
            return Json(objparam, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public ActionResult GetSpecificationList(int VId)
        {
            dbPageId = 34;
            List<Specification> objparam = VariationSettingApiUtil.GetSpecificationRecordsByVariationId(CurrentCompanyId, VId).Result.Specifications;
            return Json(objparam, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public ActionResult GetInventoryQTYDetails(string id)
        {
            dbPageId = 34;
            List<WarehouseInventory> objparam = GetWarehouseInventories(Convert.ToInt32(id));
            return Json(objparam, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public ActionResult GetCurrencyList()
        {
            dbPageId = 34;
            var model = new InventoryOtherCurrencyPrice();
            var objparam = model.CurrencyList();
            return Json(objparam, JsonRequestBehavior.AllowGet);
        }

        public string GetEbayMarketPlace()
        {
            dbPageId = 34;
            var result = new InventoryViewModel() { };

            MarketPlaceSettingsApiResponse eBayResponse = MarketPlaceSettingsApiUtil.GetAllEbaySettingRecords(CurrentCompanyId).Result;
            if (eBayResponse.IsSuccessful)
            {
                result.EbaySettings = eBayResponse.EbaySettings;
            }
            return Serialize(result);
        }

        public string GetAmazonMarketPlace()
        {
            dbPageId = 34;
            var result = new InventoryViewModel() { };

            MarketPlaceSettingsApiResponse AmazonResponse = MarketPlaceSettingsApiUtil.GetAllAmazonMWSSettingRecords(CurrentCompanyId).Result;

            if (AmazonResponse.IsSuccessful)
            {
                result.AmazonMWSSettings = AmazonResponse.AmazonMWSSettings;
            }
            return Serialize(result);
        }

        public static string Serialize(object obj)
        {
            string result = JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore, PreserveReferencesHandling = PreserveReferencesHandling.None });
            return result;
        }

        public JsonResult SaveEbayMarketPlace(string model)
        {
            dbPageId = 34;
            try
            {
                var result = JsonConvert.DeserializeObject<List<ViewListing>>(model);
                foreach (var item in result)
                {
                    try
                    {
                        if (CurrentBaseCompanyId == CurrentCompanyId)
                        {
                            item.CreatedByUserId = CurrentUserId;
                            item.UpdatedByUserId = CurrentUserId;
                        }
                        else
                        {
                            item.CreatedByUserId = SystemUserId;
                            item.UpdatedByUserId = SystemUserId;
                        }

                        item.Status = "NOT Listed";
                        ViewListingApiResponse viewListingApiResponse = ViewListingApiUtil.AddViewListing(CurrentCompanyId, item).Result;
                    }
                    catch (Exception ex)
                    {
                        return Json(false, JsonRequestBehavior.AllowGet);
                    }
                }
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }

        }
        public JsonResult SaveGrouponMarketPlace(string model)
        {
            dbPageId = 34;
            try
            {
                var result = JsonConvert.DeserializeObject<List<GrouponStores>>(model);
                foreach (var item in result)
                {
                    try
                    {
                        if (CurrentBaseCompanyId == CurrentCompanyId)
                        {
                            item.CreatedByUserId = CurrentUserId;
                            item.UpdatedByUserId = CurrentUserId;
                        }
                        else
                        {
                            item.CreatedByUserId = SystemUserId;
                            item.UpdatedByUserId = SystemUserId;
                        }

                        ViewListingApiResponse viewListingApiResponse = ViewListingApiUtil.AddGrouponListing(CurrentCompanyId, item).Result;
                    }
                    catch (Exception ex)
                    {
                        return Json(false, JsonRequestBehavior.AllowGet);
                    }
                }
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult SaveWalmartMarketPlace(string model)
        {
            dbPageId = 34;
            try
            {
                var result = JsonConvert.DeserializeObject<List<ListingWalMart>>(model);
                foreach (var item in result)
                {
                    try
                    {
                        if (CurrentBaseCompanyId == CurrentCompanyId)
                        {
                            item.CreatedByUserId = CurrentUserId;
                            item.UpdatedByUserId = CurrentUserId;
                        }
                        else
                        {
                            item.CreatedByUserId = SystemUserId;
                            item.UpdatedByUserId = SystemUserId;
                        }

                        ViewListingApiResponse viewListingApiResponse = ViewListingApiUtil.AddWalMartListing(CurrentCompanyId, item).Result;
                    }
                    catch (Exception ex)
                    {
                        return Json(false, JsonRequestBehavior.AllowGet);
                    }
                }
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult SaveAmazonMarketPlace(string model)
        {
            dbPageId = 34;
            try
            {
                var result = JsonConvert.DeserializeObject<List<AmazonViewListing>>(model);
                foreach (var item in result)
                {
                    try
                    {
                        if (CurrentBaseCompanyId == CurrentCompanyId)
                        {
                            item.CreatedByUserId = CurrentUserId;
                            item.UpdatedByUserId = CurrentUserId;
                        }
                        else
                        {
                            item.CreatedByUserId = SystemUserId;
                            item.UpdatedByUserId = SystemUserId;
                        }
                        ViewListingApiResponse isExist = ViewListingApiUtil.IsExistAmazon(CurrentCompanyId, item).Result;
                        if (isExist.IsSuccessful)
                            return Json(new { IsSuccess = "exists" }, JsonRequestBehavior.AllowGet);
                        else
                        {
                            ViewListingApiResponse viewListingApiResponse = ViewListingApiUtil.AddAmazonViewListing(CurrentCompanyId, item).Result;
                        }
                    }
                    catch (Exception ex)
                    {
                        return Json(new { IsSuccess = "false" }, JsonRequestBehavior.AllowGet);
                    }
                }
                return Json(new { IsSuccess = "true" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { IsSuccess = "false" }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult SendtoUpdateWarehouseQTY(List<int> ids)
        {
            dbPageId = 34;
            Session["ProductsWarehouseQTY"] = ids;
            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult UpdateWarehouseQTY()
        {
            dbPageId = 34;
            var model = new ProductsWithWarehouseQTYViewModel()
            {
                Warehouses = new List<Warehouse>(),
                ProductsWithWarehouseQTYs = new List<ProductsWithWarehouseQTY>()
            };
            if (Session["ProductsWarehouseQTY"] != null)
            {
                var ids = Session["ProductsWarehouseQTY"] as List<int>;
                Session["ProductsWarehouseQTY"] = null;
                if (ids != null) model.ProductsWithWarehouseQTYs = GetAllInventoryRecordsForUpdateWarehouseQTY(ids);
                else model.ProductsWithWarehouseQTYs = GetAllInventoryRecordsForUpdateWarehouseQTY(null);
            }
            else model.ProductsWithWarehouseQTYs = GetAllInventoryRecordsForUpdateWarehouseQTY(null);

            model.Warehouses = GetWarehouseLists().Where(w => w.WarehouseType != "Fulfillment Warehouse").ToList();
            return View(model);
        }
        [HttpPost]
        public ActionResult UpdateWarehouseQTY(List<ProductsWithWarehouseQTY> objWarehouseQTYs)
        {
            dbPageId = 34;
            var inventoryApiResponse = InventoryApiUtil.UpdateInventoryRecordsForUpdateWarehouseQTY(CurrentCompanyId, objWarehouseQTYs).Result;
            if (inventoryApiResponse.IsSuccessful)
                return Json("ok", JsonRequestBehavior.AllowGet);
            else
                return Json("fail", JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult SendtoUpdateProductsPriceandCost(List<int> ids)
        {
            dbPageId = 34;
            Session["ProductsPriceandCost"] = ids;
            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult UpdateProductsPriceandCost()
        {
            dbPageId = 34;
            var model = new List<ProductsWithPriceandCost>();
            if (Session["ProductsPriceandCost"] != null)
            {
                var ids = Session["ProductsPriceandCost"] as List<int>;
                Session["ProductsPriceandCost"] = null;
                if (ids != null) model = GetAllInventoryRecordsForUpdatePriceandCost(ids);
                else model = GetAllInventoryRecordsForUpdatePriceandCost(null);
            }
            else model = GetAllInventoryRecordsForUpdatePriceandCost(null);
            return View(model);
        }
        [HttpPost]
        public ActionResult UpdateProductsPriceandCost(List<ProductsWithPriceandCost> objPriceCosts)
        {
            dbPageId = 34;
            var inventoryApiResponse = InventoryApiUtil.UpdateInventoryRecordsForUpdatePriceandCost(CurrentCompanyId, objPriceCosts).Result;
            if (inventoryApiResponse.IsSuccessful)
                return Json("ok", JsonRequestBehavior.AllowGet);
            else
                return Json("fail", JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult SendtoCreateOrderUsingSKU(List<int> ids)
        {
            dbPageId = 34;
            Session["CreateOrderUsingSKU"] = ids;
            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult SendtoCreatePOUsingSKU(List<int> ids)
        {
            dbPageId = 34;
            Session["CreatePOUsingSKU"] = ids;
            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public ActionResult GetWarehouseList()
        {
            dbPageId = 34;
            var result = GetWarehouseLists();
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        private List<Warehouse> GetWarehouseLists()
        {
            var Warehouses = WarehouseApiUtil.GetAllWarehouseRecords(CurrentCompanyId).Result.Warehouses;
            if (Warehouses != null)
                return Warehouses;
            else
                return new List<Warehouse>();
        }
        private List<CustomFields> GetCustomFieldLists()
        {
            var CustomFields = CustomfieldsApiUtil.GetCustomfieldsRecords(CurrentCompanyId, "Inventory").Result.CustomFields;
            if (CustomFields != null)
                return CustomFields;
            else
                return new List<CustomFields>();
        }
        private List<ProductsWithWarehouseQTY> GetAllInventoryRecordsForUpdateWarehouseQTY(List<int> ids)
        {
            var inventoryApiResponse = InventoryApiUtil.GetAllInventoryRecordsForUpdateWarehouseQTY(CurrentCompanyId, ids).Result;
            if (inventoryApiResponse.ProductsWithWarehouseQTYs != null)
                return inventoryApiResponse.ProductsWithWarehouseQTYs;
            else
                return new List<ProductsWithWarehouseQTY>();
        }
        private List<ProductsWithPriceandCost> GetAllInventoryRecordsForUpdatePriceandCost(List<int> ids)
        {
            var inventoryApiResponse = InventoryApiUtil.GetAllInventoryRecordsForUpdatePriceandCost(CurrentCompanyId, ids).Result;
            if (inventoryApiResponse.ProductsWithPriceandCosts != null)
                return inventoryApiResponse.ProductsWithPriceandCosts;
            else
                return new List<ProductsWithPriceandCost>();
        }
        private List<Manufacturer> GetManufacturerList()
        {
            var response = ManufacturerApiUtil.GetAllManufacturerRecords(CurrentCompanyId).Result.Manufacturers;
            if (response != null)
                return response;
            else
                return new List<Manufacturer>();
        }

        private List<Brand> GetBrandList()
        {
            var response = BrandApiUtil.GetAllBrandRecords(CurrentCompanyId).Result.Brands;
            if (response != null)
                return response;
            else
                return new List<Brand>();
        }
        private List<CustomFieldInventory> GetCustomFieldInventories(int inventoryId)
        {
            var customFieldInventoryApiResponse = CustomFieldInventoryApiUtil.GetCustomFieldInventoryByinventoryId(CurrentCompanyId, inventoryId).Result;
            if (customFieldInventoryApiResponse.CustomFieldInventories != null)
                return customFieldInventoryApiResponse.CustomFieldInventories;
            else
                return new List<CustomFieldInventory>();
        }

        public ActionResult GetAllImagesFromAmazonAPI(InventoryViewModel model)
        {
            List<ProductModel> response = new List<ProductModel>();
            var ASIN = model.ASIN.Split(',');
            try
            {
                dbPageId = 34;
                MarketPlaceSettingsApiResponse Response = MarketPlaceSettingsApiUtil.GetAmazonMWSSettingRecord(CurrentCompanyId, model.AmzProfileID).Result;
                var AmazonSetting = Response.AmazonMWSSetting;
                var objMWS = new AmazonMWS.MWS(AmazonSetting.Product_Adv_API_AccessKeyID, AmazonSetting.Product_Adv_API_SecretKey, AmazonSetting.AmazonSellerID);
                var Images = objMWS.FetchImagesFromAmazon(ASIN[0]);
                if (ModelState.IsValid)
                {
                    if (Images != null && Images.Count > 0)
                    {
                        int i = 1;
                        model.InventoryImages = new List<InventoryImage>();
                        foreach (var item in Images)
                        {
                            var Ext = Path.GetExtension(item);
                            var inputFileName = "Image_Amazon_" + i + Ext;
                            var filePath = "~/Uploads/" + CurrentCompanyId + "/InventoryImages/" + ASIN[1] + "/";
                            var directoryPath = Server.MapPath(filePath);
                            if (!Directory.Exists(directoryPath))
                            {
                                Directory.CreateDirectory(directoryPath);
                            }
                            var serverSavePath = Path.Combine(directoryPath + inputFileName);
                            using (WebClient wc = new WebClient())
                            {
                                wc.DownloadFile(item, serverSavePath);
                            }
                            System.Drawing.Image img = System.Drawing.Image.FromFile(serverSavePath);
                            int height = img.Height;
                            int width = img.Width;
                            string ext = Path.GetExtension(serverSavePath);
                            filePath = filePath + inputFileName;
                            if (CurrentBaseCompanyId == CurrentCompanyId)
                            {
                                model.InventoryImages.Add(new InventoryImage { InventoryId = Convert.ToInt32(ASIN[1]), ImageName = inputFileName, ImagePath = filePath, ImageWidth = width, ImageHeight = height, Status = "A", CreatedByUserId = CurrentUserId, UpdatedByUserId = CurrentUserId });
                            }
                            else
                            {
                                model.InventoryImages.Add(new InventoryImage { InventoryId = Convert.ToInt32(ASIN[1]), ImageName = inputFileName, ImagePath = filePath, ImageWidth = width, ImageHeight = height, Status = "A", CreatedByUserId = SystemUserId, UpdatedByUserId = SystemUserId });
                            }
                            i++;
                        }
                    }
                    WebApiResponse webApiResponse = InventoryApiUtil.AddInventoryImages(CurrentCompanyId, model.InventoryImages).Result;
                    if (webApiResponse.IsSuccessful)
                    {
                        return Json(true, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return PartialView(response);
        }

        public ActionResult CreateMultipleSKU()
        {
            dbPageId = 34;
            if (CurrentBaseCompanyId == CurrentCompanyId)
                ViewBag.UserId = CurrentUserId;
            else
                ViewBag.UserId = SystemUserId;
            return View();
        }

        [HttpPost]
        public ActionResult CreateMultipleSKU(MulitpleSKUViewModel model)
        {
            dbPageId = 34;
            var message = "";
            var webApiResponse = InventoryApiUtil.AddMultipleSKU(CurrentCompanyId, model.MulitpleSKUs).Result;
            if (webApiResponse.IsSuccessful)
            {
                if (webApiResponse.MulitpleSKUs == null)
                    message = ShowMessage(MessageType.success, $"Successfully added all SKUs. <a href='#'>View SKU</a>");
                else
                    message = ShowMessage(MessageType.success, $"The SKU='{string.Join(",", webApiResponse.MulitpleSKUs.Select(m => m.SKU))}' you are trying to add already exists and duplicate SKU are forbidden. Please try a Different SKU. <a href='#'>View SKU</a>");

                return Json(new { status = "ok", message = message }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                message = ShowMessage(MessageType.danger, $"Something went wrong!. {webApiResponse.Message}. Some of SKUs not created {string.Join(",", webApiResponse.MulitpleSKUs.Select(m => m.SKU))}");
                return Json(new { status = "fail", message = message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public ActionResult FindFilterKitsList(int id)
        {
            dbPageId = 34;
            var model = new InventoryViewModel
            {
                InventoryImages = new List<InventoryImage>(),
                Alias = new List<InventoryAlias>(),
                Kits = new List<InventoryKit>(),
                Orders = new List<Order>(),
                PurchaseOrders = new List<PurchaseOrder>(),
                ProductMarkers = new List<ProductMarker>(),
                InventoryVariations = new List<InventoryVariation>(),
                InventorySKULogs = new List<InventorySKULog>(),
                WarehouseInventories = new List<WarehouseInventory>(),
                WarehouseTransfers = new List<WarehouseTransfer>(),
                OtherCurrencyPrices = new List<InventoryOtherCurrencyPrice>(),
                InventoryBuyers = new List<InventoryBuyer>(),
                Users = new List<CommerceBitUser>(),
                InventoryVendorSKUs = new List<InventoryVendorSKU>(),
                BrandList = new List<SelectListItem>(),
                ManufactureList = new List<SelectListItem>()
            };
            try
            {
                var inventoryApiResponse = InventoryApiUtil.GetInventoryRecord(CurrentCompanyId, id).Result;
                model.Inventory = inventoryApiResponse.Inventory;

                if (model.Inventory.InventoryKits != null && model.Inventory.InventoryKits.Count > 0)
                {
                    model.Kits.AddRange(model.Inventory.InventoryKits);
                }


                if (model.Inventory.InventoryVendorSKUs != null && model.Inventory.InventoryVendorSKUs.Count > 0)
                {
                    model.InventoryVendorSKUs.AddRange(model.Inventory.InventoryVendorSKUs);
                }

                model.Inventory.Weight = (model.Inventory.IsOZorLBS == false ? model.Inventory.WeightLBS : model.Inventory.WeightOZ);
                model.Inventory = inventoryApiResponse.Inventory;
                //Get Orders
                model.Orders = GetOrderRecordsByInventoryId(id, 1, 5);
                //Get Orders
                model.PurchaseOrders = GetPurchaseOrderRecordsByInventoryId(id, 1, 15);

                //Get Inventories
                model.Inventories = new List<Inventory>();
                model.Inventories = GetInventories();

                //Get Users
                model.Users = GetUsers();

                //Get KitTypes
                model.KitTypes = GetKitTypes();
                //Get Warehouses
                model.Warehouses = GetWarehouseLists();
                //Get Warehouses
                model.WarehouseInventories = GetWarehouseInventories(id); //warehouselist.Join(inventorywarehouseList, w => w.Warehouse.WarehouseId, i => i.WarehouseId, (w, i) => new { warehouselist = w, inventorywarehouseList = i }).Select(x => new WarehouseInventory { WarehouseInventoryId = x.warehouselist.WarehouseInventoryId, WarehouseId = x.warehouselist.WarehouseId, InventoryId = x.warehouselist.InventoryId, Warehouse = x.warehouselist.Warehouse, Quantity = x.warehouselist.Quantity, ProductLocation = x.warehouselist.ProductLocation } ).ToList(); //.Select(x => x.inventorywarehouseList).ToList()
                                                                          //Get Warehouses Transfer
                model.WarehouseTransfers = GetWarehouseTransfers(id);
                model.BrandList = GetBrandList().Select(p => new SelectListItem { Text = p.Name, Value = p.Name }).ToList();
                model.ManufactureList = GetManufacturerList().Select(p => new SelectListItem { Text = p.Name, Value = p.Name }).ToList();
            }
            catch
            {
            }
            return PartialView("_FindFilterKitsList", model);
        }

        [HttpGet]
        public ActionResult FindFilterAliasList(int id)
        {
            dbPageId = 34;
            var model = new InventoryViewModel
            {
                InventoryImages = new List<InventoryImage>(),
                Alias = new List<InventoryAlias>(),
                Kits = new List<InventoryKit>(),
                Orders = new List<Order>(),
                PurchaseOrders = new List<PurchaseOrder>(),
                ProductMarkers = new List<ProductMarker>(),
                InventoryVariations = new List<InventoryVariation>(),
                InventorySKULogs = new List<InventorySKULog>(),
                WarehouseInventories = new List<WarehouseInventory>(),
                WarehouseTransfers = new List<WarehouseTransfer>(),
                OtherCurrencyPrices = new List<InventoryOtherCurrencyPrice>(),
                InventoryBuyers = new List<InventoryBuyer>(),
                Users = new List<CommerceBitUser>(),
                InventoryVendorSKUs = new List<InventoryVendorSKU>(),
                BrandList = new List<SelectListItem>(),
                ManufactureList = new List<SelectListItem>()
            };
            try
            {
                var inventoryApiResponse = InventoryApiUtil.GetInventoryRecord(CurrentCompanyId, id).Result;
                model.Inventory = inventoryApiResponse.Inventory;

                if (model.Inventory.InventoryAliases != null && model.Inventory.InventoryAliases.Count > 0)
                {
                    model.Alias.AddRange(model.Inventory.InventoryAliases);
                }
                if (model.Inventory.InventoryVendorSKUs != null && model.Inventory.InventoryVendorSKUs.Count > 0)
                {
                    model.InventoryVendorSKUs.AddRange(model.Inventory.InventoryVendorSKUs);
                }
                model.Inventory.Weight = (model.Inventory.IsOZorLBS == false ? model.Inventory.WeightLBS : model.Inventory.WeightOZ);
                model.Inventory = inventoryApiResponse.Inventory;
                //Get Orders
                model.Orders = GetOrderRecordsByInventoryId(id, 1, 5);
                //Get Orders
                model.PurchaseOrders = GetPurchaseOrderRecordsByInventoryId(id, 1, 15);

                //Get Inventories
                model.Inventories = new List<Inventory>();
                model.Inventories = GetInventories();

                //Get Users
                model.Users = GetUsers();

                //Get KitTypes
                model.KitTypes = GetKitTypes();
                //Get Warehouses
                model.Warehouses = GetWarehouseLists();
                //Get Warehouses
                model.WarehouseInventories = GetWarehouseInventories(id); //warehouselist.Join(inventorywarehouseList, w => w.Warehouse.WarehouseId, i => i.WarehouseId, (w, i) => new { warehouselist = w, inventorywarehouseList = i }).Select(x => new WarehouseInventory { WarehouseInventoryId = x.warehouselist.WarehouseInventoryId, WarehouseId = x.warehouselist.WarehouseId, InventoryId = x.warehouselist.InventoryId, Warehouse = x.warehouselist.Warehouse, Quantity = x.warehouselist.Quantity, ProductLocation = x.warehouselist.ProductLocation } ).ToList(); //.Select(x => x.inventorywarehouseList).ToList()
                                                                          //Get Warehouses Transfer
                model.WarehouseTransfers = GetWarehouseTransfers(id);
                model.BrandList = GetBrandList().Select(p => new SelectListItem { Text = p.Name, Value = p.Name }).ToList();
                model.ManufactureList = GetManufacturerList().Select(p => new SelectListItem { Text = p.Name, Value = p.Name }).ToList();
            }
            catch
            {
            }
            return PartialView("_FindFilterAliasList", model);
        }

        #region Brand
        [HttpPost]
        public ActionResult AddBrand(Brand brand)
        {
            dbPageId = 34;
            if (CurrentBaseCompanyId == CurrentCompanyId)
            {
                brand.CreatedByUserId = CurrentUserId;
                brand.UpdatedByUserId = CurrentUserId;
            }
            else
            {
                brand.CreatedByUserId = SystemUserId;
                brand.UpdatedByUserId = SystemUserId;
            }
            var message = "";
            var result = "";

            if (ModelState.IsValid)
            {
                var response = BrandApiUtil.AddEditBrand(CurrentCompanyId, brand).Result;
                if (response.IsSuccessful)
                {
                    result = "ok";
                    message = $"Brand '{brand.Name}' submitted successfully.";
                }
                else
                {
                    result = "fail";
                    message = response.Message;
                }
            }
            else
            {
                var errorMessage = "";
                foreach (ModelState modelState in ViewData.ModelState.Values)
                {
                    foreach (ModelError error in modelState.Errors)
                    {
                        errorMessage = errorMessage + error.ErrorMessage.ToString();
                    }
                }
                result = "fail";
                message = errorMessage;
            }

            return Json(new { result = result, message = message }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult AddManufacturer(Manufacturer manufacturer)
        {
            dbPageId = 34;
            if (CurrentBaseCompanyId == CurrentCompanyId)
            {
                manufacturer.CreatedByUserId = CurrentUserId;
                manufacturer.UpdatedByUserId = CurrentUserId;
            }
            else
            {
                manufacturer.CreatedByUserId = SystemUserId;
                manufacturer.UpdatedByUserId = SystemUserId;
            }
            var message = "";
            var result = "";

            if (ModelState.IsValid)
            {
                var response = ManufacturerApiUtil.AddEditManufacturer(CurrentCompanyId, manufacturer).Result;
                if (response.IsSuccessful)
                {
                    result = "ok";
                    message = $"Manufacturer '{manufacturer.Name}' submitted successfully.";
                }
                else
                {
                    result = "fail";
                    message = response.Message;
                }
            }
            else
            {
                var errorMessage = "";
                foreach (ModelState modelState in ViewData.ModelState.Values)
                {
                    foreach (ModelError error in modelState.Errors)
                    {
                        errorMessage = errorMessage + error.ErrorMessage.ToString();
                    }
                }
                result = "fail";
                message = errorMessage;
            }

            return Json(new { result = result, message = message }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region WarehouseInventoryLog

        [HttpGet]
        public ActionResult GetWarehouseInventoryLogDetails(string id)
        {
            dbPageId = 34;
            var response = WarehouseInventoryLogApiUtil.GetWarehouseInventoryLogByInventoryId(CurrentCompanyId, Convert.ToInt32(id)).Result;
            if (response.WarehouseInventoryLogs != null)
                return Json(response.WarehouseInventoryLogs, JsonRequestBehavior.AllowGet);
            return Json(new List<WarehouseInventoryLog>(), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AddWarehouseInventoryLog(WarehouseInventoryLog wiLog)
        {
            dbPageId = 34;
            if (CurrentBaseCompanyId == CurrentCompanyId)
            {
                wiLog.CreatedByUserId = CurrentUserId;
                wiLog.UpdatedByUserId = CurrentUserId;
            }
            else
            {
                wiLog.CreatedByUserId = SystemUserId;
                wiLog.UpdatedByUserId = SystemUserId;
            }
            var message = "";
            var result = "";

            if (ModelState.IsValid)
            {
                var response = WarehouseInventoryLogApiUtil.AddWarehouseInventoryLog(CurrentCompanyId, wiLog).Result;
                if (response.IsSuccessful)
                {
                    result = "ok";
                    message = $"QTY updated successfully.";
                }
                else
                {
                    result = "fail";
                    message = response.Message;
                }
            }
            else
            {
                var errorMessage = "";
                foreach (ModelState modelState in ViewData.ModelState.Values)
                {
                    foreach (ModelError error in modelState.Errors)
                    {
                        errorMessage = errorMessage + error.ErrorMessage.ToString();
                    }
                }
                result = "fail";
                message = errorMessage;
            }

            return Json(new { result = result, message = message }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        public ActionResult CreateSKUFromWalmart()
        {
            dbPageId = 34;
            var Data = Session["WalmartSetting"] as List<WalMartSetting>;
            if (Data == null)
            {
                MarketPlaceSettingsApiResponse WalmartResponse = MarketPlaceSettingsApiUtil.GetAllWalMartRecords(CurrentCompanyId).Result;
                if (WalmartResponse.IsSuccessful)
                    Data = WalmartResponse.WalMartList;
            }
            ViewBag.WalmartSetting = Data;
            return View();
        }

        public ActionResult FetchWalmartDataFromAPI(string Query, int AmazonID)
        {
            List<ProductModel> response = new List<ProductModel>();
            try
            {
                dbPageId = 34;
                MarketPlaceSettingsApiResponse Response = MarketPlaceSettingsApiUtil.GetAmazonMWSSettingRecord(CurrentCompanyId, AmazonID).Result;
                var AmazonSetting = Response.AmazonMWSSetting;
                var objMWS = new AmazonMWS.MWS(AmazonSetting.AWSAccessKeyID, AmazonSetting.AmazonSecretKey, AmazonSetting.AmazonSellerID);
                response = objMWS.ListMatchingProduct(Query, AmazonSetting.AmazonMarketplaceID);
            }
            catch (Exception ex)
            {
            }
            return PartialView(response);
        }
    }
}
}