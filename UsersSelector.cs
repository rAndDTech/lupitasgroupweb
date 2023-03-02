using DonauMorgen.NEXTFI.Web.DAL;
using DonauMorgen.NEXTFI.Web.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml;

namespace DonauMorgen.NEXTFI.Web.Classes
{
    public class UsersSelector
    {
        private ApplicationDbContext dbContext = new ApplicationDbContext();
        private NEXTFIDBContext db = new NEXTFIDBContext();

        
        //Get List<Roles>
        public List<Role> Roles()
        {
            List<Role> ListRoles = new List<Role> {
                 new Role(){ Name="Accounting"},
                new Role(){ Name="Associates"},
                new Role(){ Name="Customer Service"},
                new Role(){ Name="Finance"},
                new Role(){ Name="Purchase Manager"},
                new Role(){ Name="Office Coordinator"},
                new Role(){ Name="Salesperson"},
                new Role(){ Name="Technician"},
                new Role(){ Name="QC Inspectors"},
                new Role(){ Name="Reconditioning Manager"}
            };
            return ListRoles;
        }

        public List<Role> RolesSubTitle()
        {
            List<Role> ListRoles = new List<Role> {
                 new Role(){ Name="CEO"},
                new Role(){ Name="General Manager"},
                new Role(){ Name="Finance Manager"},
                new Role(){ Name="Store Manager"},
                new Role(){ Name="Employee"},
              
            };
            return ListRoles;
        }

        //GetRole of User
        public string GetRole(string userId)
        {
           
            var getRole = dbContext.Roles.Where(x => x.Users.Select(y => y.UserId).Contains(userId)).FirstOrDefault();
            if (getRole != null)
                return getRole.Name;
            return "";
        }

        //GetName of User
        public string GetUserName(string userId)
        {
            if (!String.IsNullOrWhiteSpace(userId))
            {
                var getUser = (dynamic)null;
                var User = dbContext.Roles.Where(x => x.Users.Select(y => y.UserId).Contains(userId)).FirstOrDefault();
                if (User != null)
                {
                    if (User.Name == "Salesperson" || User.Name == "Technician" || User.Name == "QC Inspectors"
                                || User.Name == "Customer Service" || User.Name == "Purchase Manager" || User.Name == "Reconditioning Manager"
                                || User.Name == "Finance" || User.Name == "Associates" || User.Name == "Accounting" || User.Name == "Office Coordinator")
                    {
                        getUser = db.Employees.Where(x => x.UserId == userId).FirstOrDefault();
                        if (getUser != null) { return String.Format("{0}{1}{2}", getUser.Name, " ", getUser.LastName); }
                    }
                    else if (User.Name == "Administrator")
                    {
                        getUser = db.Administrators.Where(x => x.UserId == userId).FirstOrDefault();
                        if (getUser != null) { return getUser.Name; }
                    }
                }
            }
            return "";
        }

        public bool changeRole(ApplicationUser user, String Role, ApplicationUserManager userManager)
        {
            var oldUser = userManager.FindById(user.Id);
            var oldRoleId = oldUser.Roles.SingleOrDefault().RoleId;
            var oldRoleName = dbContext.Roles.AsNoTracking().SingleOrDefault(r => r.Id == oldRoleId).Name;

            if (oldRoleName != Role)
            {
                userManager.RemoveFromRole(user.Id, oldRoleName);
                userManager.AddToRole(user.Id, Role);

            }

            dbContext.Entry(user).State = EntityState.Modified;
            return true;
        }

        public int GetPermissions(Guid UserId,String Area)
        {
            int permission = 0;
            var xmlPath = Path.Combine(HttpRuntime.AppDomainAppPath, "LupFiles/Settings/UsersConfig.xml");
            
            XmlDocument UsersConfig = new XmlDocument();
            try
            {
                UsersConfig.Load(xmlPath);
                XmlNodeList nodeList = UsersConfig.SelectNodes("config/"+UserId.ToString());
                foreach (XmlNode DL in nodeList)
                {
                    XmlNode Node = DL.SelectSingleNode(Area);
                    if (Node!=null)
                    {
                        switch (Node.InnerText.ToString())
                        {
                            case "FullAccess":
                                permission = 1;
                                break;
                            case "NoAccess":
                                permission = 3;
                                break;
                            default:
                                permission = 0;
                                break;
                        }

                    }
                }
            }
            catch
            {
            }
               
            return permission;
        }
    }
}