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
                SyncInfo currentSyncInfo = db.SyncInfos.OrderByDescending(x => x.SyncDateTime).FirstOrDefault();
                SyncInfo syncInfo = new SyncInfo();
                if (currentSyncInfo == null || currentSyncInfo.ID == request.lastSyncID)
                {
                    syncInfo.SyncDateTime = DateTime.Now;
                    syncInfo.UpdatedDate = DateTime.Now;
                    db.SyncInfos.InsertOnSubmit(syncInfo);
                    db.SubmitChanges();
                }

                //get data added after last sync id
                var newData = db.Lectures.Where(s => s.SyncID == null);
                List<RequestLecture> newLectures = new List<RequestLecture>();
                foreach (var x in newData)
                {
                    RequestLecture objNewLecture = new RequestLecture();
                    objNewLecture.id = x.ID;
                    objNewLecture.name = x.Name;
                    objNewLecture.SyncID = syncInfo.ID;

                    Lecturer newLecturer = db.Lecturers.Where(s => s.Lecture_ID == objNewLecture.id).FirstOrDefault();
                    RequestLecturer objNewLecturer = new RequestLecturer();
                    objNewLecturer.id = newLecturer.ID;
                    objNewLecturer.name = newLecturer.Name;
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
                        reqStudents.Add(requestStudent);
                    }
                    objNewLecture.lecturer.students = reqStudents;
                    newLectures.Add(objNewLecture);
                    x.SyncID = syncInfo.ID;
                    db.SubmitChanges();
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
                        objLecture.SyncID = syncInfo.ID;
                        db.Lectures.InsertOnSubmit(objLecture);
                    }
                    else
                    {
                        objLecture.Name = x.name;
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
                        db.Lecturers.InsertOnSubmit(objlecturer);
                    }
                    else
                    {
                        objlecturer.Name = x.lecturer.name;
                        objlecturer.Lecture_ID = objLecture.ID;
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
                            db.Students.InsertOnSubmit(objStudent);
                        }
                        else
                        {
                            objStudent.Name = s.name;
                        }                        
                        db.SubmitChanges();

                        Lecturer_Student objLecturerStudent = db.Lecturer_Students.FirstOrDefault(a => a.Student_ID == objStudent.ID && a.Lecturer_ID == objlecturer.ID);
                        if(objLecturerStudent == null)
                        {
                            objLecturerStudent = new Lecturer_Student();
                            objLecturerStudent.ID = s.lectureStudentID;// == Guid.Empty ? Guid.NewGuid() : objLecturerStudent.ID;
                            objLecturerStudent.Student_ID = objStudent.ID;
                            objLecturerStudent.Lecturer_ID = objlecturer.ID;
                            db.Lecturer_Students.InsertOnSubmit(objLecturerStudent);
                        }
                        else
                        {
                            objLecturerStudent.Student_ID = objStudent.ID;
                            objLecturerStudent.Lecturer_ID = objlecturer.ID;
                        }
                        db.SubmitChanges();
                    }
                }
                return json;
            }
        }
    }
}
