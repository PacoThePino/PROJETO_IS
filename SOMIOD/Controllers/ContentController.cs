using SOMIOD.App.Helpers;
using SOMIOD.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.Http;
using System.Web.Http.Results;

namespace SOMIOD.Controllers
{
    // Rota com 3 níveis!
    [RoutePrefix("api/somiod/{appName}/{containerName}")]
    public class ContentController : ApiController
    {
        // =============================================================
        // LER DADO (GET): .../{containerName}/{recordName}
        // =============================================================
        [HttpGet]
        [Route("{recordName}")]
        public IHttpActionResult GetRecord(string appName, string containerName, string recordName)
        {
            // JOIN triplo para garantir a hierarquia App -> Container -> Dado
            string query = @"
                SELECT CI.* FROM ContentInstance CI
                JOIN Container C ON CI.ParentContainerId = C.Id
                JOIN Application A ON C.ParentAppId = A.Id
                WHERE A.Name = @AppName AND C.Name = @ContainerName AND CI.Name = @RecordName";

            List<SqlParameter> param = new List<SqlParameter>
            {
                new SqlParameter("@AppName", appName),
                new SqlParameter("@ContainerName", containerName),
                new SqlParameter("@RecordName", recordName)
            };

            var dt = SqlDataHelper.ExecuteQuery(query, param);

            if (dt.Rows.Count == 0) return NotFound();

            ContentInstance ci = new ContentInstance
            {
                Id = (int)dt.Rows[0]["Id"],
                Name = (string)dt.Rows[0]["Name"],
                CreationDate = (DateTime)dt.Rows[0]["CreationDate"],
                Content = (string)dt.Rows[0]["Content"],
                ContentType = (string)dt.Rows[0]["ContentType"],
                ResType = "content-instance"
            };

            return Ok(ci);
        }

        // =============================================================
        // APAGAR DADO (DELETE): .../{containerName}/{recordName}
        // =============================================================
        [HttpDelete]
        [Route("{recordName}")]
        public IHttpActionResult DeleteRecord(string appName, string containerName, string recordName)
        {

            var checkResult = GetRecord(appName, containerName, recordName);
            if (!(checkResult is System.Web.Http.Results.OkNegotiatedContentResult<ContentInstance> okResult))
                return NotFound();

            ContentInstance dataToDelete = okResult.Content;

            string query = @"
                DELETE CI 
                FROM ContentInstance CI
                JOIN Container C ON CI.ParentContainerId = C.Id
                JOIN Application A ON C.ParentAppId = A.Id
                WHERE A.Name = @AppName AND C.Name = @ContainerName AND CI.Name = @RecordName";

            List<SqlParameter> param = new List<SqlParameter>
            {
                new SqlParameter("@AppName", appName),
                new SqlParameter("@ContainerName", containerName),
                new SqlParameter("@RecordName", recordName)
            };


            int rows = SqlDataHelper.ExecuteNonQuery(query, param);

            if (rows > 0)
            {
                // =============================================================
                // << GATILHO DE ELIMINAÇÃO >>
                // Passamos 'true' para o isDeletion para o serviço saber que o evento é 2
                // =============================================================
                Services.NotificationService.FireEvent(appName, containerName, dataToDelete, 2);

                return Ok("Dado apagado com sucesso.");
            }

            return NotFound();
        }
    }
}