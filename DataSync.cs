using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SyncAPI
{
    public class DataSync
    {
    }

    public class DataSyncRequest
    {
        public int? lastSyncID { get; set; }
        public List<RequestLecture> lectures { get; set; }
        public List<RequestLecturerStudent> lecturerStudents { get; set; }
    }

    public class RequestLecture
    {
        public Guid id { get; set; }
        public string name { get; set; }
        public int SyncID { get; set; }
        public RequestLecturer lecturer { get; set; }
    }

    public class RequestLecturer
    {
        public Guid id { get; set; }
        public string name { get; set; }
        public List<RequestStudent> students { get; set; }
    }

    public class RequestStudent
    {
        public Guid id { get; set; }
        public string name { get; set; }
        public Guid lectureStudentID { get; set; }
    }

    public class RequestLecturerStudent
    {
        public Guid id { get; set; }
        public Guid? Lecturer_ID { get; set; }
        public Guid? Student_ID { get; set; }
    }
}
