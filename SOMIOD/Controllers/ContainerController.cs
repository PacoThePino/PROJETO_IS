using SOMIOD.App.Helpers;
using SOMIOD.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Web.Http;

namespace SOMIOD.Controllers
{
    // Define que este controlador trata de URLs: api/somiod/NOME_APP/
    [RoutePrefix("api/somiod/{appName}")]
    public class ContainerController : ApiController
    {
        // =============================================================
        // LER CONTAINER (GET): api/somiod/{appName}/{containerName}
        // =============================================================
        [HttpGet]
        [Route("{containerName}")]
        public IHttpActionResult GetContainer(string appName, string containerName)
        {
            // O SQL junta as tabelas para garantir que o Container pertence à App pedida
            string query = @"
                SELECT C.Id, C.Name, C.CreationDate, C.ParentAppId 
                FROM Container C
                JOIN Application A ON C.ParentAppId = A.Id
                WHERE A.Name = @AppName AND C.Name = @ContainerName";

            List<SqlParameter> paramsList = new List<SqlParameter>
            {
                new SqlParameter("@AppName", appName),
                new SqlParameter("@ContainerName", containerName)
            };

            var dt = SqlDataHelper.ExecuteQuery(query, paramsList);

            if (dt.Rows.Count == 0) return NotFound();

            Container container = new Container
            {
                Id = (int)dt.Rows[0]["Id"],
                Name = (string)dt.Rows[0]["Name"],
                CreationDate = (DateTime)dt.Rows[0]["CreationDate"],
                ParentAppId = (int)dt.Rows[0]["ParentAppId"],
                ResType = "container"
            };

            return Ok(container);
        }

        // =============================================================
        // ATUALIZAR CONTAINER (PUT): api/somiod/{appName}/{containerName}
        // =============================================================
        [HttpPut]
        [Route("{containerName}")]
        public IHttpActionResult UpdateContainer(string appName, string containerName, [FromBody] Container container)
        {
            if (container == null || string.IsNullOrEmpty(container.Name))
                return BadRequest("Novo nome é obrigatório.");

            // 1. Verificar se existe e buscar a Data Original
            string queryCheck = @"
                SELECT C.Id, C.CreationDate 
                FROM Container C
                JOIN Application A ON C.ParentAppId = A.Id
                WHERE A.Name = @AppName AND C.Name = @ContainerName";

            List<SqlParameter> paramsCheck = new List<SqlParameter>
            {
                new SqlParameter("@AppName", appName),
                new SqlParameter("@ContainerName", containerName)
            };

            var dt = SqlDataHelper.ExecuteQuery(queryCheck, paramsCheck);

            if (dt.Rows.Count == 0) return NotFound();

            // Guardamos a data original para devolver no final
            DateTime originalDate = (DateTime)dt.Rows[0]["CreationDate"];

            // 2. Tentar Atualizar o Nome
            try
            {
                string queryUpdate = @"
                    UPDATE C
                    SET C.Name = @NewName
                    FROM Container C
                    JOIN Application A ON C.ParentAppId = A.Id
                    WHERE A.Name = @AppName AND C.Name = @OldName";

                List<SqlParameter> paramsUpdate = new List<SqlParameter>
                {
                    new SqlParameter("@NewName", container.Name),
                    new SqlParameter("@AppName", appName),
                    new SqlParameter("@OldName", containerName) 
                };

                SqlDataHelper.ExecuteNonQuery(queryUpdate, paramsUpdate);

                // Prepara o objeto de resposta com os dados certos
                container.CreationDate = originalDate;
                container.ResType = "container";

                return Ok(container);
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627) return Conflict(); // Nome já existe
                return InternalServerError(ex);
            }
        }

        // =============================================================
        // APAGAR CONTAINER (DELETE): api/somiod/{appName}/{containerName}
        // =============================================================
        [HttpDelete]
        [Route("{containerName}")]
        public IHttpActionResult DeleteContainer(string appName, string containerName)
        {
            string query = @"
                DELETE C
                FROM Container C
                JOIN Application A ON C.ParentAppId = A.Id
                WHERE A.Name = @AppName AND C.Name = @ContainerName";

            List<SqlParameter> paramsList = new List<SqlParameter>
            {
                new SqlParameter("@AppName", appName),
                new SqlParameter("@ContainerName", containerName)
            };

            int rows = SqlDataHelper.ExecuteNonQuery(query, paramsList);

            if (rows == 0) return NotFound();

            return Ok("Container apagado com sucesso.");
        }
    }
}