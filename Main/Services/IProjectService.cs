using FluorescenceFullAutomatic.Model;
using FluorescenceFullAutomatic.Sql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Services
{
    public interface IProjectService
    {
        Task<List<Project>> GetProjects(bool isDefault);
        Project GetProject(int id);

        bool UpdateProject(Project project);
    }
    public class ProjectService : IProjectService
    {
        public bool UpdateProject(Project project)
        {
            return SqlHelper.getInstance().UpdateProject(project);
        }
        public Task<List<Project>> GetProjects(bool isDefault)
        {
            return SqlHelper.getInstance().GetAllProjectAsync(isDefault);
        }

        public Project GetProject(int id)
        {
            return SqlHelper.getInstance().GetProjectForID(id);
        }
    }
}
