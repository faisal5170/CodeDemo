//using ET.Model.ServiceModel;
//using ET.Services.Business;
//using ET.Services.Contracts.Entities;
//using ET.Services.Contracts.ServiceInterfaces;
using System.Collections.Generic;
using System;
namespace CustomCodingStyle.Models
{
    public class CoreService //: ICourse
    {
        public Course CreateCourse(Course item)
        {
            return FaultHandler<Course>.Protect(() => new CourseDomain().Create(item));
        }

        public Course GetCourse(int id)
        {
            return FaultHandler<Course>.Protect(() => new CourseDomain().Get(id));
        }

        public IEnumerable<Course> ListCourses(int trainerId)
        {
            return FaultHandler<Course>.Protect(() => new CourseDomain().List(trainerId));
        }

        public IEnumerable<string> ListByPrefix(string prefix)
        {
            return FaultHandler<string>.Protect(() => new CourseDomain().ListByPrefix(prefix));
        }

        public IEnumerable<Course> ListCoursesByIds(IEnumerable<int> courseIds)
        {
            return FaultHandler<Course>.Protect(() => new CourseDomain().ListByCourseIds(courseIds));
        }

        public IEnumerable<Course> ListCategoryByIds(IEnumerable<int> categoryId)
        {
            return FaultHandler<Course>.Protect(() => new CourseDomain().ListByCategoryIds(categoryId));
        }

        public void UpdateCourse(Course item)
        {
            FaultHandler<Course>.Protect(() => new CourseDomain().Update(item));
        }

        public void UpdateCourseStatusSendEmail(Course item)
        {
            FaultHandler<Course>.Protect(() => new CourseDomain().UpdateCourseStatusSendEmail(item));
        }
    }
}