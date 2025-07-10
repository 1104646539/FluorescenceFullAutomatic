using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Model;
using FluorescenceFullAutomatic.Sql;

namespace FluorescenceFullAutomatic.Services
{
    public interface IProjectService
    {
        Task<List<Project>> GetAllProjectAsync(bool isDefault);
        List<Project> GetAllProject();
        Project GetProjectForID(int id);
        bool UpdateProject(Project project);

         Project GetProjectForQrcode(string qrcode);
    }
    public class ProjectRepository : IProjectService
    {
        public ProjectRepository()
        {
        }

        public List<Project> GetAllProject()
        {
            return SqlHelper.getInstance().GetAllProject();
        }

        public Task<List<Project>> GetAllProjectAsync(bool isDefault)
        {
            return SqlHelper.getInstance().GetAllProjectAsync(isDefault);
        }

        public Project GetProjectForID(int id)
        {
            return SqlHelper.getInstance().GetProjectForID(id);
        }

        public Project GetProjectForQrcode(string qrcode)
        {
                   // 解析二维码
            if (string.IsNullOrEmpty(qrcode))
            {
                return null;
            }
           
            Project project = SqlHelper.getInstance().GetProjectForQRCode(qrcode);
            
            return project;
        }

        public bool UpdateProject(Project project)
        {
            return SqlHelper.getInstance().UpdateProject(project);
        }
    }
}
