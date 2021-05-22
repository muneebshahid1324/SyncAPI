using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccess;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace SyncAPI.Controllers
{
    [ApiController]
    [Route("DataSync")]
    public class DataSyncController : ControllerBase
    {
        private IConfiguration Configuration;

        public DataSyncController(IConfiguration _configuration)
        {
            Configuration = _configuration;
        }

        [HttpPost]
        [Route("SyncLecturer")]
        public string SyncLecturer(DataSyncRequest request)
        {
            using (SyncDBDataContext db = new SyncDBDataContext(this.Configuration.GetConnectionString("SyncDBConn")))
            {
                DateTime? syncDateTime = DateTime.Now;

                //get data added after last sync id
                var newData = db.Lectures.Where(s => (request.lastSyncDateTime == null || s.UpdatedDate > request.lastSyncDateTime));
                List<RequestLecture> newLectures = new List<RequestLecture>();
                foreach (var x in newData)
                {
                    RequestLecture objNewLecture = new RequestLecture();
                    objNewLecture.id = x.ID;
                    objNewLecture.name = x.Name;
                    objNewLecture.UpdatedDate = x.UpdatedDate;
                    objNewLecture.CreatedDate = x.CreatedDate;

                    Lecturer newLecturer = db.Lecturers.Where(s => s.Lecture_ID == objNewLecture.id).FirstOrDefault();
                    RequestLecturer objNewLecturer = new RequestLecturer();
                    objNewLecturer.id = newLecturer.ID;
                    objNewLecturer.name = newLecturer.Name;
                    objNewLecturer.UpdatedDate = newLecturer.UpdatedDate;
                    objNewLecturer.CreatedDate = newLecturer.CreatedDate;
                    objNewLecture.lecturer = objNewLecturer;

                    List<Lecturer_Student> newLecturerStudents = db.Lecturer_Students.Where(s => s.Lecturer_ID == objNewLecturer.id).ToList<Lecturer_Student>();
                    List<RequestLecturerStudent> newReqLecturerStudents = new List<RequestLecturerStudent>();
                    List<RequestStudent> reqStudents = new List<RequestStudent>();
                    foreach (var ls in newLecturerStudents)
                    {
                        Student newStudent = db.Students.Where(x => x.ID == ls.Student_ID).FirstOrDefault();
                        RequestStudent requestStudent = new RequestStudent();
                        requestStudent.id = newStudent.ID;
                        requestStudent.name = newStudent.Name;
                        requestStudent.UpdatedDate = newStudent.UpdatedDate;
                        requestStudent.CreatedDate = newStudent.CreatedDate;
                        reqStudents.Add(requestStudent);
                    }
                    objNewLecture.lecturer.students = reqStudents;
                    newLectures.Add(objNewLecture);
                }
                var json = JsonConvert.SerializeObject(newLectures);

                //add new data
                foreach (var x in request.lectures)
                {
                    Lecture objLecture = db.Lectures.FirstOrDefault(a => a.ID == x.id);
                    if (objLecture == null)
                    {
                        objLecture = new Lecture();
                        objLecture.ID = x.id;// == Guid.Empty ? Guid.NewGuid() : x.id;
                        objLecture.Name = x.name;
                        objLecture.UpdatedDate = x.UpdatedDate;
                        objLecture.CreatedDate = x.CreatedDate;
                        db.Lectures.InsertOnSubmit(objLecture);
                    }
                    else
                    {
                        objLecture.Name = x.name;
                        objLecture.UpdatedDate = x.UpdatedDate;
                    }
                    db.SubmitChanges();

                    Lecturer objlecturer = db.Lecturers.FirstOrDefault(a => a.Lecture_ID == objLecture.ID && a.ID == x.lecturer.id);
                    if(objlecturer == null)
                    {
                        Lecturer oldLecturer = db.Lecturers.FirstOrDefault(a => a.Lecture_ID == objLecture.ID);
                        if(oldLecturer != null) {
                            db.Lecturers.DeleteOnSubmit(oldLecturer);
                        }
                        objlecturer = new Lecturer();
                        objlecturer.ID = x.lecturer.id;// == Guid.Empty ? Guid.NewGuid() : x.lecturer.id;
                        objlecturer.Name = x.lecturer.name;
                        objlecturer.Lecture_ID = objLecture.ID;
                        objlecturer.CreatedDate = x.lecturer.CreatedDate;
                        objlecturer.UpdatedDate = x.lecturer.UpdatedDate;
                        db.Lecturers.InsertOnSubmit(objlecturer);
                    }
                    else
                    {
                        objlecturer.Name = x.lecturer.name;
                        objlecturer.Lecture_ID = objLecture.ID;
                        objlecturer.UpdatedDate = x.lecturer.UpdatedDate;
                    }
                    db.SubmitChanges();

                    List<Lecturer_Student> lecturer_Students = new List<Lecturer_Student>();
                    foreach (var s in x.lecturer.students)
                    {
                        Student objStudent = db.Students.FirstOrDefault(a => a.ID == s.id);
                        if(objStudent == null)
                        {
                            objStudent = new Student();
                            objStudent.ID = s.id;// == Guid.Empty ? Guid.NewGuid() : s.id;
                            objStudent.Name = s.name;
                            objStudent.CreatedDate = s.CreatedDate;
                            objStudent.UpdatedDate = s.UpdatedDate;
                            db.Students.InsertOnSubmit(objStudent);
                        }
                        else
                        {
                            objStudent.Name = s.name;
                            objStudent.UpdatedDate = s.UpdatedDate;
                        }                        
                        db.SubmitChanges();

                        Lecturer_Student objLecturerStudent = db.Lecturer_Students.FirstOrDefault(a => a.Student_ID == objStudent.ID && a.Lecturer_ID == objlecturer.ID);
                        if(objLecturerStudent == null)
                        {
                            objLecturerStudent = new Lecturer_Student();
                            objLecturerStudent.ID = s.lectureStudentID;// == Guid.Empty ? Guid.NewGuid() : objLecturerStudent.ID;
                            objLecturerStudent.Student_ID = objStudent.ID;
                            objLecturerStudent.Lecturer_ID = objlecturer.ID;
                            objLecturerStudent.CreatedDate = syncDateTime;
                            objLecturerStudent.UpdatedDate = syncDateTime;
                            db.Lecturer_Students.InsertOnSubmit(objLecturerStudent);
                        }
                        else
                        {
                            objLecturerStudent.Student_ID = objStudent.ID;
                            objLecturerStudent.Lecturer_ID = objlecturer.ID;
                            objLecturerStudent.UpdatedDate = syncDateTime;
                        }
                        db.SubmitChanges();
                    }
                }
                return json;
            }
        }
    }
}
