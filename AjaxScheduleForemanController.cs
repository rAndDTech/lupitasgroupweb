using DonauMorgen.NEXTFI.Web.Classes.Services;
using DonauMorgen.NEXTFI.Web.DAL;
using DonauMorgen.NEXTFI.Web.Models;
using DonauMorgen.NEXTFI.Web.Models.Response;
using DonauMorgen.NEXTFI.Web.Models.ViewModels;
using DonauMorgen.NEXTFI.Web.Settings;
using DonauMorgen.NEXTFI.Web.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DonauMorgen.NEXTFI.Web.AJAX
{
    public class AjaxScheduleForemanController : Controller
    {
        ScheduleForemanService scheduleForemanService = new ScheduleForemanService();
        ChangeDayRequestsService changeDayRequestsService = new ChangeDayRequestsService();

        private NEXTFIDBContext db = new NEXTFIDBContext();
        EmployeeScheduleService employeeScheduleService = new EmployeeScheduleService();

        [HttpGet]
        public JsonResult AddUpdateScheduleForeman(string UserId, string Time,string WeekDay)//weekDay
        {
            List<EmployeesModel> Employees = db.Employees.Where(x => x.LogicalDelete==0).ToList();
            ResponseModel responseModel = new ResponseModel();
            for (int i=0;i<Employees.Count;i++)
            {
                UserId = Employees[i].UserId;//134c4de1-71b4-445f-aace-e2a9bbbecc0e 7e9db32e-d7d1-4b0e-a3a6-42894a805e16
                List<EmployeeScheduleModel> employeesAssigments = employeeScheduleService.GetEmployeesByWeekDay(WeekDay).Where(item=>item.UserId==UserId).ToList();
                if (employeesAssigments.Count()>0 && IsAdmon(UserId)==false)
                {
                    if (ExistScheduleForeman(UserId) == false)
                    {
                        ScheduleForemanModel scheduleForemanModel = new ScheduleForemanModel
                        {
                            Date = DateTime.Now,
                            Time = Time,
                            UserId = UserId,
                            Counter = 1,
                            IsActive=0
                        };

                        responseModel.success = scheduleForemanService.CreateEntity(scheduleForemanModel);
                        responseModel.message = "Foreman Created";
                        //send message de bienvenida
                        //new AjaxMessageTwilioController().SendMessageForeman(UserId, "ScheduleEmployee003");
                    }
                    else
                    {
                        //esta creado y hay que ver las fechas
                        ScheduleForemanModel sc = scheduleForemanService.ExistEntity(UserId);
                        if (sc != null)
                        {
                            if (sc.IsActive == 1)
                            { 
                                if (sc.Date.Year < DateTime.Now.Year || sc.Date.Month < DateTime.Now.Month || sc.Date.Day < DateTime.Now.Day) //comparar fecha sin hora
                                {
                                    //update fecha, reset counter send message welcome
                                    sc.Date = DateTime.Now;
                                    sc.Time = Time;
                                    sc.Counter = 1;
                                    responseModel.success = scheduleForemanService.UpdateEntity(sc);
                                    //send message de bienvenida
                                    new AjaxMessageTwilioController().SendMessageForeman(UserId, "ScheduleEmployee003");
                                }
                                else
                                {
                                    int lastTime = Convert24To12toInt(Time);
                                    //Get assignments by day previous to current time with undone status
                                    List<EmployeeScheduleModel> employees = employeeScheduleService.GetEmployeesByWeekDay(WeekDay).Where(item => item.UserId == UserId && item.EndTime <= lastTime && item.Done == false && item.Done100 == false && item.Done50 == false && item.Pending == false).ToList();
                                    //Check if the current day is on ChangeDay
                                    List<ChangeDayRequestsModel> changes = changeDayRequestsService.ReadAllBy(UserId).Where(item => item.Date.Year == DateTime.Now.Year && item.Date.Month == DateTime.Now.Month && item.Date.Day == DateTime.Now.Day).ToList();
                                    if (new GeneralSettingsController().HoursDifference(Time, sc.Time) > 1)
                                    {
                                        if (employees.Count > 0 && changes.Count == 0)
                                        {
                                            //actualizar hora y contador
                                            sc.Time = Time;
                                            sc.Counter ++;
                                            //envia mensaje porque es dia de trabajo y ese dia trabaja no tiene permisos
                                            //(sc.Counter % 4 == 0) send  meessage by employee and administrator
                                            if (sc.Counter % 4 != 0)
                                            {
                                                new AjaxMessageTwilioController().SendMessageForeman(UserId, "ScheduleEmployee004", employees.Count);

                                            }
                                            else
                                            
                                            {
                                                //verificar el rol de Store Manager, Admin, Master Account
                                                new AjaxMessageTwilioController().SendMessageForeman(UserId, "ScheduleEmployee005", employees.Count);

                                                //mensajes para admons
                                                List<EmployeesModel> EmployeesModel = db.Employees.Where(x => x.LogicalDelete == 0 && x.UserId != sc.UserId).ToList();
                                                EmployeesModel employee = db.Employees.Where(x => x.LogicalDelete == 0 && x.UserId == sc.UserId).FirstOrDefault();
                                                if (sc.IsActiveMaster == 1)
                                                {
                                                    new MessagesClass().SendMessageForeman(MessagesClass.PhoneMaster, employee, "ScheduleEmployee006", false, employees.Count);
                                                }
                                                foreach (var item in EmployeesModel)
                                                {
                                                    if (IsAdmon(item.UserId))
                                                    {
                                                        if (sc.IsActiveMaster==1 && item.Subtitle == "CEO")
                                                        {
                                                            new MessagesClass().SendMessageForeman(MessagesClass.PhoneMaster, employee, "ScheduleEmployee006", false, employees.Count);
                                                        }
                                                        if (sc.IsActiveManager == 1 && item.Subtitle=="General Manager")
                                                        {
                                                            new MessagesClass().SendMessageForeman(item.Phone, employee, "ScheduleEmployee006", false, employees.Count);
                                                        }
                                                        if (sc.IsActiveAdmin == 1 && item.Subtitle == "Finance Manager")
                                                        {
                                                            new MessagesClass().SendMessageForeman(item.Phone, employee, "ScheduleEmployee006", false, employees.Count);
                                                        }
                                                        if (sc.IsActiveAdmin == 1 && item.Subtitle == "Store Manager")
                                                        {
                                                            new MessagesClass().SendMessageForeman(item.Phone, employee, "ScheduleEmployee006", false, employees.Count);
                                                        }

                                                    }    
                                                }
                                            }
                                            responseModel.success = scheduleForemanService.UpdateEntity(sc);
                                        }
                                    }
                                    responseModel.data = new { lastTime, employees };
                                }
                            }
                            responseModel.success = true;
                            responseModel.message = "";

                        }
                    }
                }
                else
                {
                    //Check if is Administrator Schedule 
                    if (employeesAssigments.Count() > 0 && IsAdmon(UserId))
                    {
                        if (ExistScheduleForeman(UserId) == false)
                        {
                            ScheduleForemanModel scheduleForemanModel = new ScheduleForemanModel
                            {
                                Date = DateTime.Now,
                                Time = Time,
                                UserId = UserId,
                                Counter = 1,
                                IsActive = 0
                            };
                            responseModel.success = scheduleForemanService.CreateEntity(scheduleForemanModel);
                            responseModel.message = "Foreman Created";
                            //send message de bienvenida
                        }
                        else
                        {
                            //esta creado y hay que ver las fechas
                            ScheduleForemanModel sc = scheduleForemanService.ExistEntity(UserId);
                            if (sc != null)
                            {
                                if (sc.IsActive == 1)
                                {
                                    if (sc.Date.Year < DateTime.Now.Year || sc.Date.Month < DateTime.Now.Month || sc.Date.Day < DateTime.Now.Day) //comparar fecha sin hora
                                    {
                                        //update fecha, reset counter send message welcome
                                        sc.Date = DateTime.Now;
                                        sc.Time = Time;
                                        sc.Counter = 1;
                                        responseModel.success = scheduleForemanService.UpdateEntity(sc);
                                        //send message de bienvenida
                                        //new AjaxMessageTwilioController().SendMessageForeman(UserId, "ScheduleEmployee003");
                                    }
                                    else
                                    {
                                        int lastTime = Convert24To12toInt(Time);
                                        //Get assignments by day previous to current time with undone status
                                        List<EmployeeScheduleModel> employees = employeeScheduleService.GetEmployeesByWeekDay(WeekDay).Where(item => item.UserId == UserId && item.EndTime <= lastTime && item.Done == false && item.Done100 == false && item.Done50 == false && item.Pending == false).ToList();
                                        //Check if the current day is on ChangeDay
                                        List<ChangeDayRequestsModel> changes = changeDayRequestsService.ReadAllBy(UserId).Where(item => item.Date.Year == DateTime.Now.Year && item.Date.Month == DateTime.Now.Month && item.Date.Day == DateTime.Now.Day).ToList();
                                        if (new GeneralSettingsController().HoursDifference(Time, sc.Time) > 1)
                                        {
                                            if (employees.Count > 0 && changes.Count == 0)
                                            {

                                                //actualizar hora y contador
                                                sc.Time = Time;
                                                sc.Counter++;
                                                //envia mensaje porque es dia de trabajo y ese dia trabaja no tiene permisos
                                                //(sc.Counter % 4 == 0) send  meessage by employee and administrator
                                                if (sc.Counter % 4 == 0)
                                                {
                                                    List<EmployeesModel> EmployeesModel = db.Employees.Where(x => x.LogicalDelete == 0 && x.UserId != sc.UserId).ToList();
                                                    EmployeesModel employee = db.Employees.Where(x => x.LogicalDelete == 0 && x.UserId == sc.UserId).FirstOrDefault();
                                                    if (sc.IsActiveMaster == 1)
                                                    {
                                                        new MessagesClass().SendMessageForeman(MessagesClass.PhoneMaster, employee, "ScheduleEmployee006", false, employees.Count);
                                                    }
                                                    foreach (var item in EmployeesModel)
                                                    {
                                                        if (IsAdmon(item.UserId))
                                                        {
                                                            if (sc.IsActiveMaster == 1 && item.Subtitle == "CEO")
                                                            {
                                                                new MessagesClass().SendMessageForeman(MessagesClass.PhoneMaster, employee, "ScheduleEmployee006", false, employees.Count);
                                                            }
                                                            if (sc.IsActiveManager == 1 && item.Subtitle == "General Manager")
                                                            {
                                                                new MessagesClass().SendMessageForeman(item.Phone, employee, "ScheduleEmployee006", false, employees.Count);
                                                            }
                                                            if (sc.IsActiveAdmin == 1 && item.Subtitle == "Finance Manager")
                                                            {
                                                                new MessagesClass().SendMessageForeman(item.Phone, employee, "ScheduleEmployee006", false, employees.Count);
                                                            }
                                                            if (sc.IsActiveAdmin == 1 && item.Subtitle == "Store Manager")
                                                            {
                                                                new MessagesClass().SendMessageForeman(item.Phone, employee, "ScheduleEmployee006", false, employees.Count);
                                                            }
                                                        }
                                                    }
                                                    //new MessagesClass().SendMessageForeman(MessagesClass.PhoneMaster, Employees[i], "ScheduleEmployee006", false, employees.Count());
                                                }
                                                responseModel.success = scheduleForemanService.UpdateEntity(sc);
                                            }
                                        }
                                        responseModel.data = new { lastTime, employees };
                                    }
                                }
                                responseModel.success = true;
                                responseModel.message = "";

                            }
                        }
                    }
                    responseModel.success = true;
                    responseModel.message = "";
                }
            }
            return responseModel.Ok();
        }

        [HttpGet]
        public JsonResult ActiveScheduleForeman(string UserId, string Time, string WeekDay,int active, int manager,int finance, int admin, int master)//
        {
           
            ResponseModel responseModel = new ResponseModel();
            if (ExistScheduleForeman(UserId) == false && IsAdmon(UserId) == false)
            {
                    ScheduleForemanModel scheduleForemanModel = new ScheduleForemanModel
                    {
                        Date = DateTime.Now,
                        Time = Time,
                        UserId = UserId,
                        Counter = 1,
                        IsActive = active,
                        IsActiveAdmin=admin,
                        IsActiveFinance=finance,
                        IsActiveManager=manager,
                        IsActiveMaster=master

                    };
                    responseModel.success = scheduleForemanService.CreateEntity(scheduleForemanModel);
                    responseModel.message = "Foreman Created";  
            }
            else
                {
                    //esta creado y hay que ver las fechas
                    ScheduleForemanModel sc = scheduleForemanService.ExistEntity(UserId);
                    if (sc != null)
                    {
                        //update fecha, reset counter send message welcome
                        sc.Counter = 1;
                        sc.IsActive = active;
                        sc.IsActiveAdmin = admin;
                        sc.IsActiveFinance = finance;
                        sc.IsActiveManager = manager;
                        sc.IsActiveMaster = master;
                        responseModel.success = scheduleForemanService.UpdateEntity(sc);

                    }
                }
                /*if (active == 1)
                    new AjaxMessageTwilioController().SendMessageForeman(UserId, "ScheduleEmployee003");
            */
                responseModel.success = true;
                responseModel.message = "";
            
            return responseModel.Ok();
        }

        [HttpGet]
        public JsonResult GetStatusForeman(string UserId)
        {

            ResponseModel responseModel = new ResponseModel();
            ScheduleForemanModel sc = scheduleForemanService.ExistEntity(UserId);

            responseModel.success = true;
            responseModel.message= sc==null?"Is Null":"";
            EmployeesModel EmployeesModel = db.Employees.Where(x => x.LogicalDelete == 0 && x.UserId==UserId).FirstOrDefault();
            responseModel.data = new { sc ,EmployeesModel};

            return responseModel.Ok();
        }

        [NonAction]
        public bool IsAdmon(string UserId)
        {
            bool isAdmon = false;
            //
            DepartmentsModel Department = db.Department.Where(x => x.Name == "Sales").FirstOrDefault();
            var ViewstAutorizations = db.Database.SqlQuery<AuthorizationsXView_ViewModel>("SELECT * FROM AuthorizationsXView_View Where Department_Id='" + Department.Id + "' AND UserId = '" + UserId + "'").ToList();
            foreach (var item2 in ViewstAutorizations)
            {
                if (item2.Name == "Schedule Week")
                {
                    
                    isAdmon = item2.AccessLevel==1;
                    break;
                }
            }
            return isAdmon;
        }
      
        [NonAction]
        public bool ExistScheduleForeman(string UserId)
        {
            var result= scheduleForemanService.ExistEntity(UserId) != null;
            return result;
        }
        [NonAction]
        public int Convert24To12toInt(string Time)
        {
            string time = new GeneralSettingsController().Convert24To12(Time);
            string hours = (int.Parse(time.Split(':')[0])>9?"":"0")+time.Split(':')[0];
            string amPmFormat = time.Split(':')[1].Split(' ')[1];
            int minute = int.Parse(time.Split(':')[1].Split(' ')[0]);
            Dictionary<string, int> times = new Dictionary<string, int>()
            {
                {"09:30 AM",1},
                {"10:00 AM",2},
                {"10:30 AM",3},
                {"11:00 AM",4},
                {"11:30 AM",5},
                {"12:00 PM",6},
                {"12:30 PM",7},
                {"01:00 PM",8},
                {"01:30 PM",9},
                {"02:00 PM",10},
                {"02:30 PM",11},
                {"03:00 PM",12},
                {"03:30 PM",13},
                {"04:00 PM",14},
                {"04:30 PM",15},
                {"05:00 PM",16},
                {"05:30 PM",17},
                {"06:00 PM",18},
                {"06:30 PM",19},
                {"07:00 PM",20},
                {"08:00 PM",21},
                {"08:30 PM",21},
                {"09:00 PM",21},
                {"09:30 PM",21},
                {"10:00 PM",21},
                {"10:30 PM",21},
                {"11:00 PM",21},
                {"11:30 PM",21},
                {"12:00 AM",21},
            };
            string minutes = (minute > 30) ? "30" : "00";
            return times[String.Format("{0}:{1} {2}", hours, minutes, amPmFormat)];

            
        }
    }
}