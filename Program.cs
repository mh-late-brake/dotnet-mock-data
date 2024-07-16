using Newtonsoft.Json;
using System.Globalization;
using System.Text;

namespace MockData;

class Program
{
    static void Main()
    {
        MockData mockData = new MockData();
        List<Schedule> schedules = mockData.GenerateSchedules();
        string schedulesJson = JsonConvert.SerializeObject(schedules, Formatting.Indented);
        Console.WriteLine(schedulesJson);
    }
}


public class MockData
{
    private List<Slot> _slotsData;
    private List<string> _names;
    private List<string> _imageUrls;
    private List<Subject> _subjects;
    private int _numOfSchedules;
    private int _numOfExamRoomsPerSchedule;
    private int _numOfStudensPerExamRoom;
    private int _numOfSubjectsPerStudent;
    private int _numOfPhysicalRoomAtCampus;
    private int _numOfStudentAcrossSchedules;
    private DateTime _scheduleStartDate;

    public MockData(int numOfSchedule = 20, int numOfExamRoomsPerSchedule = 15, int numOfStudentsPerExamRoom = 20, int numOfSubjectPerStudent = 5, DateTime scheduleStartDate = default(DateTime), int numOfPhysicalRoomAtCampus = 30)
    {
        _numOfSchedules = numOfSchedule;
        _numOfExamRoomsPerSchedule = numOfExamRoomsPerSchedule;
        _numOfStudensPerExamRoom = numOfStudentsPerExamRoom;
        _numOfSubjectsPerStudent = numOfSubjectPerStudent;
        _scheduleStartDate = scheduleStartDate == default(DateTime) ? new DateTime(2024, 8, 1) : scheduleStartDate;
        _numOfPhysicalRoomAtCampus = numOfPhysicalRoomAtCampus;
        _numOfStudentAcrossSchedules = _numOfSchedules * _numOfExamRoomsPerSchedule * _numOfStudensPerExamRoom / _numOfSubjectsPerStudent;

        _slotsData = GetSlotsFromJson();
        _names = GetNamesFromJson();
        _imageUrls = GetImageUrlsFromJson();
        _subjects = GetSubjectsFromJson();

        if (numOfPhysicalRoomAtCampus < numOfExamRoomsPerSchedule)
        {
            throw new Exception("Logic error: numOfPhysicalRoomAtCampus < numOfExamRoomsPerSchedule");
        }
    }

    public List<Schedule> GenerateSchedules()
    {
        const string semester = "SU24";
        List<ScheduleTime> scheduleTimes = GenerateScheduleTimes();
        List<ExamRoom> examRooms = GenerateExamRoomsAcrossSchedules();

        List<Schedule> schedules = new List<Schedule>();

        for (int i = 0; i < _numOfSchedules; ++i)
        {
            ScheduleTime timeForThisSchedule = scheduleTimes[i];
            List<ExamRoom> examRoomsForThisSchedule = examRooms.Slice(i * _numOfExamRoomsPerSchedule, _numOfExamRoomsPerSchedule);

            Schedule newSchedule = new Schedule(timeForThisSchedule.StartTime, timeForThisSchedule.EndTime, examRoomsForThisSchedule, semester);
            schedules.Add(newSchedule);
        }

        return schedules;
    }

    public List<ScheduleTime> GenerateScheduleTimes()
    {
        List<ScheduleTime> scheduleTimes = new List<ScheduleTime>();

        DateTime startDate = _scheduleStartDate;

        for (int i = 0; i < _numOfSchedules; ++i)
        {
            foreach (Slot slot in _slotsData)
            {
                DateTime scheduleStartTime = startDate.Date + slot.StartTime;
                DateTime scheduleEndTime = startDate.Date + slot.EndTime;
                ScheduleTime newScheduleTime = new ScheduleTime
                {
                    StartTime = scheduleStartTime,
                    EndTime = scheduleEndTime,
                };
                scheduleTimes.Add(newScheduleTime);
            }

            startDate = startDate.AddDays(1);
        }

        return scheduleTimes;
    }

    public List<ExamRoom> GenerateExamRoomsAcrossSchedules()
    {
        // This will be looped over multiple time and re-used
        List<string> roomNames = GenerateRoomNames();

        // This doesn't change
        string title = "FE";

        // This will be looped over multiple time and re-used
        List<Student> students = GenerateStudents();

        // This will be looped over multiple time and re-used
        List<Subject> subjects = _subjects;

        int numOfExamRoomsAcrossSchedules = _numOfSchedules * _numOfExamRoomsPerSchedule;

        List<ExamRoom> examRooms = new List<ExamRoom>();

        for (int i = 0; i < numOfExamRoomsAcrossSchedules; ++i)
        {
            string roomName = roomNames[i % roomNames.Count];

            List<StudentRoomSubject> stss = new List<StudentRoomSubject>();

            for (int j = i; j < i + _numOfStudensPerExamRoom; ++j)
            {
                Student student = students[j % students.Count];
                Subject subject = subjects[j % subjects.Count];
                StudentRoomSubject sts = new StudentRoomSubject(student, subject);
                stss.Add(sts);
            }

            ExamRoom newExamRoom = new ExamRoom(roomName, title, stss);
            examRooms.Add(newExamRoom);
        }

        return examRooms;
    }

    public List<Student> GenerateStudents()
    {
        List<string> studentIds = GetStudentIds();
        List<string> allPossibleStudentName = _names;
        Shuffle(allPossibleStudentName);
        List<string> studentsCitizenIdentities = GetStudentCitizenIdentities();
        List<string> images = _imageUrls;

        List<Student> students = new List<Student>();

        for (int i = 0; i < _numOfStudentAcrossSchedules; ++i)
        {
            string fullName = allPossibleStudentName[i % allPossibleStudentName.Count];
            string email = CalculateEmail(fullName, studentIds[i]);
            int imageIndexSoThatNotOutOfRange = i % _imageUrls.Count;

            Student newStudent = new Student(studentIds[i], fullName, email, studentsCitizenIdentities[i], images[imageIndexSoThatNotOutOfRange]);

            students.Add(newStudent);
        }

        return students;
    }

    public List<string> GetStudentCitizenIdentities()
    {
        List<string> citizenIdentities = new List<string>();

        for (int i = 0; i < _numOfStudentAcrossSchedules; ++i)
        {
            string newCitizenidentity = (2017000000 + i).ToString();
            citizenIdentities.Add(newCitizenidentity);
        }

        return citizenIdentities;
    }

    public List<string> GetStudentIds()
    {
        List<string> studentIds = new List<string>();

        for (int i = 0; i < _numOfStudentAcrossSchedules; ++i)
        {
            string newStudentId = "HE18" + (4001 + i * 5).ToString();
            studentIds.Add(newStudentId);
        }

        return studentIds;
    }

    public string CalculateEmail(string fullName, string studentId)
    {
        string[] words = fullName.Split(" ");
        string firstName = RemoveAccents(words[words.Length - 1]);
        string lastName = RemoveAccents(words[0]);
        string middleName = RemoveAccents(words.Length > 2 ? words[1] : "");

        string email;
        if (middleName != "")
        {
            email = firstName + Char.ToUpper(lastName[0]) + Char.ToUpper(middleName[0]);
        }
        else
        {
            email = firstName + Char.ToUpper(lastName[0]);
        }
        email += studentId + "@fpt.edu.vn";

        return email;
    }

    public static string RemoveAccents(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Normalize the text to decompose combined characters into base characters and diacritical marks
        text = text.Normalize(NormalizationForm.FormD);

        // Create a StringBuilder to store the filtered characters
        StringBuilder stringBuilder = new StringBuilder();

        // Iterate through each character in the normalized text
        foreach (char c in text)
        {
            // Get the Unicode category of the character
            UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);

            // Add only base characters to the StringBuilder (skip diacritical marks)
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        // Normalize the result to Form C (composed form)
        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    public List<string> GenerateRoomNames()
    {
        List<string> allPossibleRoomName = GetAllPossibleRoomNames();
        Shuffle(allPossibleRoomName);
        return allPossibleRoomName.Slice(0, _numOfPhysicalRoomAtCampus);
    }

    public static void Shuffle<T>(List<T> list)
    {
        Random rng = new Random(123);
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    public List<string> GetAllPossibleRoomNames()
    {
        List<string> buildings = new List<string> { "AL", "BE", "DE" };
        List<string> floors = new List<string> { "1", "2", "3", "4", "5" };

        List<string> roomNumbers = new List<string>();
        for (int i = 1; i <= 20; ++i)
        {
            string newRoomNumber = i.ToString("00");
            roomNumbers.Add(newRoomNumber);
        }


        List<string> allPossibleRoomNames = new List<string>();

        foreach (string building in buildings)
        {
            foreach (string floor in floors)
            {
                foreach (string roomNumber in roomNumbers)
                {
                    string newRoomName = building + floor + roomNumber;
                    allPossibleRoomNames.Add(newRoomName);
                }
            }
        }

        return allPossibleRoomNames;
    }

    public static List<Slot> GetSlotsFromJson()
    {
        const string slotJsonFilePath = "./slot.json";
        return LoadJson<List<Slot>>(slotJsonFilePath);
    }

    public static List<Subject> GetSubjectsFromJson()
    {
        const string subjectJsonFilePath = "./subject.json";
        return LoadJson<List<Subject>>(subjectJsonFilePath);
    }

    public static List<string> GetNamesFromJson()
    {
        const string nameJsonFilePath = "./name.json";
        NameData nameData = LoadJson<NameData>(nameJsonFilePath);

        List<string> names = new List<string>();

        foreach (string lastName in nameData.LastNames)
        {
            foreach (string middleAndFirstName in nameData.MiddleAndFirstNames)
            {
                // lastName is họ
                // In Vietnamese name, lastName is at the beginning
                string newName = $"{lastName} {middleAndFirstName}";
                names.Add(newName);
            }
        }

        return names;
    }

    public static List<string> GetImageUrlsFromJson()
    {
        const string imageUrlJsonFilePath = "./image.json";
        return LoadJson<List<string>>(imageUrlJsonFilePath);
    }

    public static T LoadJson<T>(string filePath)
    {
        using (StreamReader r = new StreamReader(filePath))
        {
            string json = r.ReadToEnd();
            T? data = JsonConvert.DeserializeObject<T>(json);

            if (data == null)
            {
                throw new Exception("Error loading data from json file");
            }

            return data;
        }
    }

}

public class ScheduleTime
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}

public class Schedule
{
    public string Semester { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public List<ExamRoom> ExamRooms { get; set; }

    public Schedule(DateTime startTime, DateTime endTime, List<ExamRoom> examRooms, string semester = "SU24")
    {
        Semester = semester;
        StartTime = startTime;
        EndTime = endTime;
        ExamRooms = examRooms;
    }
}

public class ExamRoom
{
    public string RoomName { get; set; }
    public string Title { get; set; }
    public List<StudentRoomSubject> StudentRoomSubjects { get; set; }

    public ExamRoom(string roomName, string title, List<StudentRoomSubject> studenRoomSubjects)
    {
        RoomName = roomName;
        Title = title;
        StudentRoomSubjects = studenRoomSubjects;
    }
}

public class StudentRoomSubject
{
    public Student Student { get; set; }
    public Subject Subject { get; set; }

    public StudentRoomSubject(Student student, Subject subject)
    {
        Student = student;
        Subject = subject;
    }
}

public class Student
{
    public string StudentId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string CitizenIdentity { get; set; }
    public string Image { get; set; }

    public Student(string studentId, string fullName, string email, string citizenIdentity, string image)
    {
        StudentId = studentId;
        FullName = fullName;
        Email = email;
        CitizenIdentity = citizenIdentity;
        Image = image;
    }
}

public class NameData
{
    public List<string> LastNames { get; set; }
    public List<string> MiddleAndFirstNames { get; set; }

    public NameData(List<string> lastNames, List<string> middleAndFirstName)
    {
        LastNames = lastNames;
        MiddleAndFirstNames = middleAndFirstName;
    }
}

public class Subject
{
    public string SubjectCode { get; set; }
    public string SubjectName { get; set; }

    public Subject(string subjectCode, string subjectName)
    {
        SubjectCode = subjectCode;
        SubjectName = subjectName;
    }

    public override string ToString()
    {
        return $"Subject object: SubjectCode {SubjectCode}, SubjectName {SubjectName}";
    }
}

public class Slot
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}
