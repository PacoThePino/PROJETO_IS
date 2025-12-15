using SOMIOD.App.Helpers;
using SOMIOD.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.Http;

namespace SOMIOD.Controllers
{
    // ATENÇÃO: O prefixo inclui "/subs" no fim!
    // Isto captura URLs: api/somiod/{app}/{container}/subs/{nomeSub}
    [RoutePrefix("api/somiod/{appName}/{containerName}/subs")]
    public class SubscriptionController : ApiController
    {
        // =============================================================
        // APAGAR SUBSCRIÇÃO (DELETE): .../subs/{subName}
        // =============================================================
        [HttpDelete]
        [Route("{subName}")]
        public IHttpActionResult DeleteSubscription(string appName, string containerName, string subName)
        {
            string query = @"
                DELETE S 
                FROM Subscription S
                JOIN Container C ON S.ParentContainerId = C.Id
                JOIN Application A ON C.ParentAppId = A.Id
                WHERE A.Name = @AppName AND C.Name = @ContainerName AND S.Name = @SubName";

            List<SqlParameter> param = new List<SqlParameter>
            {
                new SqlParameter("@AppName", appName),
                new SqlParameter("@ContainerName", containerName),
                new SqlParameter("@SubName", subName)
            };

            int rows = SqlDataHelper.ExecuteNonQuery(query, param);

            if (rows == 0) return NotFound();

            return Ok("Subscrição apagada com sucesso.");
        }

        // =============================================================
        // LER SUBSCRIÇÃO (GET): .../subs/{subName}
        // =============================================================
        [HttpGet]
        [Route("{subName}")]
        public IHttpActionResult GetSubscription(string appName, string containerName, string subName)
        {
            // O enunciado diz que devemos conseguir ver os detalhes da subscrição
            string query = @"
                SELECT S.* FROM Subscription S
                JOIN Container C ON S.ParentContainerId = C.Id
                JOIN Application A ON C.ParentAppId = A.Id
                WHERE A.Name = @AppName AND C.Name = @ContainerName AND S.Name = @SubName";

            List<SqlParameter> param = new List<SqlParameter>
            {
                new SqlParameter("@AppName", appName),
                new SqlParameter("@ContainerName", containerName),
                new SqlParameter("@SubName", subName)
            };

            var dt = SqlDataHelper.ExecuteQuery(query, param);

            if (dt.Rows.Count == 0) return NotFound();

            Subscription sub = new Subscription
            {
                Id = (int)dt.Rows[0]["Id"],
                Name = (string)dt.Rows[0]["Name"],
                CreationDate = (DateTime)dt.Rows[0]["CreationDate"],
                Endpoint = (string)dt.Rows[0]["Endpoint"],
                Event = (string)dt.Rows[0]["Event"],
                ResType = "subscription"
            };

            return Ok(sub);
        }
    }
}