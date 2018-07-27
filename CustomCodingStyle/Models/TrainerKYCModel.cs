using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
//using ET.Services.Contracts.Constants;
//using ET.Services.Contracts.Entities;
//using ET.Services.Contracts.Enums;
//using ET.Services.Contracts.ServiceInterfaces;
//using ET.Web.Common;

namespace CustomCodingStyle.Models
{
    public class TrainerKYCModel
    {
        public string ErrorMessage { get; private set; }
        private readonly TrainerDocumentsStatusModel _trainerDocumentsStatusModel;
        private readonly UserEducationModel _userEducationModel;
        private readonly UserEmploymentModel _userEmploymentModel;
        private readonly TrainerDocumentModel _trainerDocumentModel;
        public TrainerDocument TrainerDocument { get; set; }
        public IEnumerable<TrainerDocument> TrainerDocuments { get; private set; }
        public IEnumerable<SelectListItem> IdentityDocuments { get; set; }
        public IEnumerable<UserEducation> UserEducations { get; private set; }
        public TrainerDocumentsStatus TrainerDocumentsStatus { get; private set; }

        public IEnumerable<UserEmployment> UserEmployments { get; private set; }
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public TrainerKYCModel()
        {
            _userEducationModel = new UserEducationModel();
            _userEmploymentModel = new UserEmploymentModel();
            _trainerDocumentModel = new TrainerDocumentModel();
            _trainerDocumentsStatusModel = new TrainerDocumentsStatusModel();
        }

        internal void Index()
        {
            var userId = Context.Current.UserId;
            Parallel.Invoke(
                          () => GetUserEducations(userId),
                          () => GetUserEmployment(userId),
                          () => GetTrainerDocuments(userId),
                          () => GetTrainerDocumentStatus(userId));
            GetIdentityDocuments();
        }

        private void GetUserEducations(int userId)
        {
            UserEducations = _userEducationModel.GetUserEducations(userId);
        }

        private void GetTrainerDocumentStatus(int userId)
        {
            TrainerDocumentsStatus = _trainerDocumentsStatusModel.TryGetTrainerDocumentsStatus(userId);
        }

        private void GetUserEmployment(int userId)
        {
            UserEmployments = _userEmploymentModel.GetUserEmployments(userId);
        }

        private void GetTrainerDocuments(int userId)
        {
            TrainerDocuments = _trainerDocumentModel.GetTrainerDocuments(userId);
        }

        internal void SaveDocuments(IEnumerable<TrainerDocument> TrainerDocument, IEnumerable<HttpPostedFileBase> UserEducation, IEnumerable<HttpPostedFileBase> UserEmployment, TrainerKYCModel model, HttpPostedFileBase TrainerIdentity)
        {
            try
            {
                ValidateUserDocuments(UserEducation);

                ValidateUserDocuments(UserEmployment);

                ValidateUserDocuments(new[] { TrainerIdentity });

                if (!string.IsNullOrEmpty(ErrorMessage)) return;

                List<TrainerDocument> documentsArray = new List<TrainerDocument>();

                int counter = 0;
                if (UserEducation != null)
                {
                    for (int i = 0; i < UserEducation.Count(); i++)
                    {
                        TrainerDocument trainerModel = GetTrainerDocument();
                        trainerModel.Id = TrainerDocument.ElementAt(counter).Id;
                        trainerModel.Location = SaveFile(UserEducation.ElementAt(i)) ?? TrainerDocument.ElementAt(counter).Location;
                        trainerModel.EducationId = TrainerDocument.ElementAt(counter).EducationId;
                        documentsArray.Add(trainerModel);
                        counter++;
                    }
                }
                if (UserEmployment != null)
                {
                    for (int i = 0; i < UserEmployment.Count(); i++)
                    {
                        TrainerDocument trainerModel = GetTrainerDocument();
                        trainerModel.Id = TrainerDocument.ElementAt(counter).Id;
                        trainerModel.Location = SaveFile(UserEmployment.ElementAt(i)) ?? TrainerDocument.ElementAt(counter).Location;
                        trainerModel.EmploymentId = TrainerDocument.ElementAt(counter).EmploymentId;
                        documentsArray.Add(trainerModel);
                        counter++;
                    }
                }

                var identityTrainerModel = GetTrainerDocument();
                identityTrainerModel.Id = model.TrainerDocument.Id;
                identityTrainerModel.Location = SaveFile(TrainerIdentity) ?? model.TrainerDocument.Location;
                identityTrainerModel.IdentityId = model.TrainerDocument.IdentityId;
                documentsArray.Add(identityTrainerModel);

                SaveTrainerDocuments(documentsArray);
            }
            catch (Exception exception)
            {
                ErrorMessage = exception.Message;
            }
        }

        internal void GetIdentityDocuments()
        {
            IdentityDocuments = (Enum.GetValues(typeof(IdentityDocument)).Cast<IdentityDocument>().Select(
                enu => new SelectListItem() { Text = enu.ToString(), Value = enu.ToString() })).ToList();
        }

        private TrainerDocument GetTrainerDocument()
        {
            var trainerDocument = new TrainerDocument();
            trainerDocument.TrainerId = Context.Current.UserId;
            trainerDocument.CreatedOn = DateTime.Today;
            return trainerDocument;
        }

        private string SaveFile(HttpPostedFileBase file)
        {
            string fileName = null;
            if (file != null)
            {
                var re = new Regex("[;\\\\/:*?\"<>|&']");
                fileName = GetCurrentUnixTimestampSeconds() + "_" + re.Replace(Path.GetFileName(file.FileName), " ").Replace(" ", "_");
                string documentPath = ConfigurationManager.AppSettings["TrainerDocument"];
                string path = Path.Combine(HttpContext.Current.Server.MapPath(documentPath), fileName);
                file.SaveAs(path);
            }
            return fileName;
        }

        private void ValidateUserDocuments(IEnumerable<HttpPostedFileBase> userDocuments)
        {
            if (userDocuments == null)
                return;

            foreach (var userDocument in userDocuments)
            {
                if (userDocument != null && userDocument.ContentLength > 0)
                {
                    var fileType = Path.GetExtension(userDocument.FileName);
                    if (!Constant.AllowedPrimeTrainersDocumentTypes.Contains(fileType.ToLower()))
                    {
                        ErrorMessage = "Please upload documents in " + Constant.AllowedPrimeTrainersDocumentTypes + " formats only";
                        break;
                    }
                    if (Constant.MaximumAllowedPrimeTrainerDocumentSizeInBytes < userDocument.ContentLength)
                    {
                        ErrorMessage = "Maximum allowed file size is " + (Constant.MaximumAllowedPrimeTrainerDocumentSizeInBytes / 1000) + " KB";
                        break;
                    }
                }
            }
        }

        private long GetCurrentUnixTimestampSeconds()
        {
            return (long)(DateTime.UtcNow - UnixEpoch).TotalSeconds;
        }

        internal void SaveTrainerDocuments(List<TrainerDocument> trainerDocuments)
        {
            try
            {
                using (var usersFactory = new WebClientFactory<ITrainerDocument>())
                {
                    var client = usersFactory.CreateClient();

                    client.SaveTrainerDocuments(trainerDocuments);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        internal string DownloadFile(string filename)
        {
            var path = Path.Combine(System.Web.HttpContext.Current.Server.MapPath(ConfigurationManager.AppSettings["TrainerDocument"]), filename);
            if (File.Exists(path))
                return ConfigurationManager.AppSettings["TrainerDocument"] + "//" + filename;
            return null;
        }
    }
}
}