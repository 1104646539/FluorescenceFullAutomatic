using FluorescenceFullAutomatic.Core.Model;
using FluorescenceFullAutomatic.Platform.Model;
using FluorescenceFullAutomatic.Platform.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestMain.Repositorys
{
    public class FakeProjectRepository : IProjectService
    {
        Dictionary<int, Project> datas = new Dictionary<int, Project>();
        int id = 0;
        public FakeProjectRepository() {
            for (int i = 0; i < 50; i++)
            {
                Project p = new Project
                {
                    ProjectCode = "FOB",
                    ProjectName = "血红蛋白",
                    ProjectUnit = "mg/L",
                    ProjectLjz = 100,
                    BatchNum = "202404"+i,
                    IdentifierCode = "FBHB001",
                    A1 = 1.2,
                    A2 = 0.8,
                    X0 = 10,
                    P = 1.5,
                    ProjectUnit2 = "ng/mL",
                    ProjectLjz2 = 80,
                    ConMax = 200,
                    A12 = 1.0,
                    A22 = 0.6,
                    X02 = 8,
                    P2 = 1.2,
                    ConMax2 = 150,
                    ProjectType = Project.Project_Type_Single,
                    TestType = Project.Test_Type_Stadard,
                    IsDefault = 0,
                    ScanStart = "100",
                    ScanEnd = "500",
                    PeakWidth = "2.5",
                    PeakDistance = "5"

                };
                id = i + 1;
                p.Id = id;

                datas.Add(id, p);
            }
        }
        public List<Project> GetAllProject()
        {
            List<Project> ps = new List<Project>();
            ps.AddRange(datas.Values);
            return ps;
        }

        public Task<List<Project>> GetAllProjectAsync(bool isDefault)
        {
            List<Project> ps = new List<Project>();
            ps.AddRange(datas.Values);
            return Task.FromResult(ps);
        }

        public Project GetProjectForID(int id)
        {
            if(datas.TryGetValue(id, out Project project))
            {
                return project;
            }
            return null;
        }

        public Project GetProjectForQrcode(string qrcode)
        {
            return datas.First().Value;
        }

        public bool UpdateProject(Project project)
        {
            if(datas.TryGetValue(project.Id, out Project existingProject))
            {
                datas[project.Id] = project;
                return true;
            }
            return false;
        }


        List<Project> IProjectService.GetAllProject()
        {
            return null;
        }

        Task<List<Project>> IProjectService.GetAllProjectAsync(bool isDefault)
        {
            return null;
        }

        Project IProjectService.GetProjectForID(int id)
        {
            return null;
        }

        Project IProjectService.GetProjectForQrcode(string qrcode)
        {
            return null;
        }
    }
}
