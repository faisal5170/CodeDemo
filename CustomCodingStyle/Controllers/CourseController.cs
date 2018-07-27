using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CustomCodingStyle.Controllers
{
    [CustomAuthorize(Roles = ApplicationFixedRoles.OnlyMasterTrainer)]
    public class CourseController : Controller //StandardController
    {
        
        //TODO: refactor no need of COurseModel.Index() calling in each action method.      
            private readonly CourseBatchModel _courseBatchModel;
            private readonly CourseModel _courseModel;
            private readonly CourseModuleModel _courseModuleModel;
            public CourseController()
            {
                _courseModel = new CourseModel();
                _courseBatchModel = new CourseBatchModel();
                _courseModuleModel = new CourseModuleModel();
            }

            [Route("courselanding/{courseid}/{coursetitle}")]
            public ActionResult CourseLanding()
            {
                _courseModel.Index();
                return View(_courseModel);
            }

            [HttpPost]
            [Route("courselanding/{courseid}/{coursetitle}")]
            public ActionResult CourseLanding(Course course)
            {
                var imageFile = Request.Files[0];
                _courseModel.UpdateCourseLandingDetails(course, imageFile);

                if (!string.IsNullOrEmpty(_courseModel.ErrorMessage))
                    TempData["Error"] = _courseModel.ErrorMessage;
                else
                    TempData["Success"] = "Landing page detail saved successfully.";

                return RedirectToAction("CourseLanding", new { courseid = Context.CourseId, coursetitle = Context.CourseTitle });
            }

            public ActionResult Create()
            {
                _courseModel.SetCategories();
                return View(_courseModel);
            }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public ActionResult Create(Course course)
            {
                var createdCourse = _courseModel.Create(course);
                if (!string.IsNullOrEmpty(_courseModel.ErrorMessage))
                {
                    ViewBag.Categories = new SelectList(_courseModel.GetCategoriesByParentCategoryId(), "Id", "Name");
                    TempData["Error"] = _courseModel.ErrorMessage;
                }
                else
                {
                    return RedirectToAction("Detail", new { courseid = createdCourse.Id, coursetitle = createdCourse.Title.FormatRouteString() });
                }
                return View();
            }

            public ActionResult CreateBatch()
            {
                return View();
            }

            [HttpPost]
            [ValidateAntiForgeryToken]
            [Route("createbatches/{courseid}/{coursetitle}")]
            public ActionResult CreateBatch(CourseBatch courseBatch, FormCollection formCollection)
            {
                var trainingDays = formCollection["_TrainingDays"];
                _courseBatchModel.Create(courseBatch, trainingDays);
                if (!string.IsNullOrEmpty(_courseBatchModel.ErrorMessage))
                    return Json(new { IsAdded = false, Message = _courseBatchModel.ErrorMessage });
                else
                {
                    return Json(new { IsAdded = true });
                }
            }

            public ActionResult Dashboard()
            {
                return View(_courseModel.List());
            }

            [Route("detail/{courseid}/{coursetitle}")]
            public ActionResult Detail()
            {
                _courseModel.Index();
                return View(_courseModel);
            }

            [HttpPost]
            [ValidateInput(false)]
            [ValidateAntiForgeryToken]
            [Route("detail/{courseid}/{coursetitle}")]
            public ActionResult Detail(Course course)
            {
                _courseModel.Update(course);
                if (!string.IsNullOrEmpty(_courseModel.ErrorMessage))
                {
                    TempData["Error"] = _courseModel.ErrorMessage;
                }
                else
                {
                    TempData["Success"] = "Course detail saved sucessfully";
                }
                _courseModel.Index();
                return View(_courseModel);
            }

            [HttpPost]
            public ActionResult GetCategoriesByParentCatgoryId(int parentCategoryId)
            {
                return Json(_courseModel.GetCategoriesByParentCategoryId(parentCategoryId));
            }

            [Route("price/{courseid}/{coursetitle}")]
            public ActionResult Price()
            {
                _courseModel.Index();
                return View(_courseModel);
            }

            [Route("price/{courseid}/{coursetitle}")]
            [HttpPost]
            public ActionResult Price(Course course)
            {
                _courseModel.UpdatePrice(course);
                if (!string.IsNullOrEmpty(_courseModel.ErrorMessage))
                {
                    TempData["Error"] = _courseModel.ErrorMessage;
                }
                else
                {
                    TempData["Success"] = "Course price saved sucessfully";
                }
                _courseModel.Index();
                return View(_courseModel);
            }



            [Route("batches/{courseid}/{coursetitle}")]
            public ActionResult Seeallbatches()
            {
                _courseBatchModel.Index(Context.CourseId);
                if (!string.IsNullOrEmpty(_courseBatchModel.ErrorMessage))
                {
                    TempData["Error"] = _courseBatchModel.ErrorMessage;
                }
                return View(_courseBatchModel);
            }

            [HttpPost]
            [ValidateAntiForgeryToken]
            [Route("submitforreview/{courseid}/{coursetitle}")]
            public ActionResult SubmitForReview()
            {
                _courseModel.UpdateCourseStatus();
                if (!string.IsNullOrEmpty(_courseModel.ErrorMessage))
                {
                    TempData["Error"] = _courseModel.ErrorMessage;
                }
                else
                {
                    TempData["Success"] = "Course is underreview";
                }
                return RedirectToAction("Detail", new { courseid = Context.CourseId, coursetitle = Context.CourseTitle.FormatRouteString() });
            }

            [Route("curriculum/{courseid}/{coursetitle}")]
            public ActionResult Curriculum()
            {
                _courseModel.Index();
                return View(_courseModel);
            }

            [HttpPost]
            [Route("curriculum/{courseid}/{coursetitle}")]
            public ActionResult SaveCurriculum(List<CourseModuleTopics> courseModule)
            {
                _courseModuleModel.SaveCourseCurriculum(courseModule);
                if (!string.IsNullOrEmpty(_courseModuleModel.ErrorMessage))
                    TempData["Error"] = _courseModuleModel.ErrorMessage;
                else
                    TempData["Success"] = "Curriculum saved sucessfully";
                return RedirectToAction("Curriculum", new { courseid = Context.CourseId, coursetitle = Context.CourseTitle.FormatRouteString() });
            }
        }
    }   
   