using SOMIOD.App.Helpers;
using SOMIOD.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using static System.Net.Mime.MediaTypeNames;
using Application = SOMIOD.Models.Application; // Define que "Application" é sempre a tua 

namespace SOMIOD.App.Controllers
{
    // Define que todos os pedidos neste controlador começam por api/somiod
    [RoutePrefix("api/somiod")]
    public class ApplicationController : ApiController
    {

        [HttpGet]
        [Route("")]
        public IHttpActionResult GetRoot()
        {
            // 1. Verificar se o Header de Discovery está presente
            IEnumerable<string> headerValues;
            if (Request.Headers.TryGetValues("somiod-discovery", out headerValues))
            {
                string resType = headerValues.FirstOrDefault();

                // Se o utilizador quer descobrir as aplicações
                if (resType.Equals("application", StringComparison.OrdinalIgnoreCase))
                {
                    List<string> paths = new List<string>();

                    // Query para buscar apenas os nomes de todas as apps
                    string query = "SELECT Name FROM Application";
                    var dt = SqlDataHelper.ExecuteQuery(query);

                    foreach (System.Data.DataRow row in dt.Rows)
                    {
                        // Adiciona o caminho relativo conforme o exemplo do enunciado
                        paths.Add($"/api/somiod/{row["Name"]}");
                    }

                    return Ok(paths); // Devolve ["/api/somiod/App1", "/api/somiod/App2"]
                }

                // Se o utilizador quiser descobrir TODAS as instâncias do sistema (Recursivo)
                if (resType.Equals("content-instance", StringComparison.OrdinalIgnoreCase))
                {
                    List<string> paths = new List<string>();
                    string query = @"
                SELECT a.Name as AppName, c.Name as ContName, ci.Name as InstName 
                FROM ContentInstance ci
                JOIN Container c ON ci.ParentContainerId = c.Id
                JOIN Application a ON c.ParentAppId = a.Id";

                    var dt = SqlDataHelper.ExecuteQuery(query);
                    foreach (System.Data.DataRow row in dt.Rows)
                    {
                        paths.Add($"/api/somiod/{row["AppName"]}/{row["ContName"]}/{row["InstName"]}");
                    }
                    return Ok(paths);
                }
            }

            // 2. Se não houver Header, o enunciado diz que o "Get All" não deve ser suportado.
            // Podes devolver um erro 400 ou uma informação básica da tua API.
            return BadRequest("Para listar recursos, use o Header 'somiod-discovery' com os valores: application ou content-instance.");
        }
        // POST: api/somiod/
        // Cria uma nova Application
        [HttpPost]
        [Route("")] // Rota vazia para apanhar apenas o prefixo api/somiod
        public IHttpActionResult CreateApplication([FromBody] Application app)
        {
            if (app == null || string.IsNullOrEmpty(app.Name))
            {
                return BadRequest("O nome da aplicação é obrigatório.");
            }

            // Validar se é "application"
            if (app.ResType != "application")
            {
                return BadRequest("Tipo de recurso inválido. Esperado: application");
            }

            try
            {   
                app.CreationDate = DateTime.Now;
                // Query SQL para inserir
                string query = "INSERT INTO Application (Name) VALUES (@Name)";

                // Parâmetros para evitar SQL Injection
                List<SqlParameter> paramsList = new List<SqlParameter>();
                // Atualiza também o parametro para enviar a data definida pelo C#
                paramsList.Add(new SqlParameter("@Date", app.CreationDate));
                paramsList.Add(new SqlParameter("@Name", app.Name));

                // Executar
                SqlDataHelper.ExecuteNonQuery(query, paramsList);

                return Ok(app); // Retorna 200 OK com os dados
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627) // Erro de chave duplicada (Nome já existe)
                {
                    return Conflict(); // 409 Conflict
                }
                return InternalServerError(ex);
            }
        }



        // ==========================================
        // LEITURA (GET): api/somiod/{name}
        // Devolve os detalhes de uma aplicação
        // ==========================================

        [HttpGet]
        [Route("{name}")]
        public IHttpActionResult GetApplication(string name)
        {
            // 1. Verificar se o Header "somiod-discovery" existe
            IEnumerable<string> headerValues;
            bool isDiscovery = Request.Headers.TryGetValues("somiod-discovery", out headerValues);

            // Buscar a App na BD para ter o ID
            string query = "SELECT * FROM Application WHERE Name = @Name";
            var dt = SqlDataHelper.ExecuteQuery(query, new List<SqlParameter> { new SqlParameter("@Name", name) });

            if (dt.Rows.Count == 0) return NotFound();
            int appId = (int)dt.Rows[0]["Id"];      

            if (isDiscovery)
            {
                string resType = headerValues.FirstOrDefault();
                return Ok(PerformDiscoveryLogic(appId, name, resType));
            }

            // Se NÃO for discovery, devolve o objeto normal (GET tradicional)
            var app = new Application
            {
                Id = appId,
                Name = (string)dt.Rows[0]["Name"],
                CreationDate = (DateTime)dt.Rows[0]["CreationDate"]
            };
            return Ok(app);
        }

        // Método auxiliar para gerar a lista de caminhos (Paths)
        private List<string> PerformDiscoveryLogic(int parentId, string parentName, string resType)
        {
            List<string> paths = new List<string>();

            if (resType == "container")
            {
                string query = "SELECT Name FROM Container WHERE ParentAppId = @id";
                var dt = SqlDataHelper.ExecuteQuery(query, new List<SqlParameter> { new SqlParameter("@id", parentId) });
                foreach (System.Data.DataRow row in dt.Rows)
                {
                    paths.Add($"/api/somiod/{parentName}/{row["Name"]}");
                }
            }
            else if (resType == "content-instance")
            {
                // Esta query busca instâncias de TODOS os containers desta App (recursivo como pede a imagem)
                string query = @"
            SELECT c.Name as ContName, ci.Name as InstName 
            FROM ContentInstance ci
            JOIN Container c ON ci.ParentContainerId = c.Id
            WHERE c.ParentAppId = @id";

                var dt = SqlDataHelper.ExecuteQuery(query, new List<SqlParameter> { new SqlParameter("@id", parentId) });
                foreach (System.Data.DataRow row in dt.Rows)
                {
                    paths.Add($"/api/somiod/{parentName}/{row["ContName"]}/{row["InstName"]}");
                }
            }
            return paths;
        }

        // ==========================================
        // ATUALIZAÇÃO (PUT): api/somiod/{name}
        // Muda o nome de uma aplicação
        // ==========================================
        [HttpPut]
        [Route("{name}")]
        public IHttpActionResult UpdateApplication(string name, [FromBody] Application app)
        {
            if (app == null || string.IsNullOrEmpty(app.Name)) return BadRequest("Novo nome obrigatório.");

            // Verifica se a app existe antes de tentar mudar
            string queryCheck = "SELECT COUNT(*) FROM Application WHERE Name = @Name";
            List<SqlParameter> paramsCheck = new List<SqlParameter> { new SqlParameter("@Name", name) };

            // Nota: Precisamos de converter o DataTable num valor simples
            var dt = SqlDataHelper.ExecuteQuery(queryCheck, paramsCheck);
            if ((int)dt.Rows[0][0] == 0) return NotFound();

            try
            {   

                // Faz com que a data de criação nao seja updated automaticamente 
                string queryGetDate = "SELECT CreationDate FROM Application WHERE Name = @OldName";
                List<SqlParameter> paramsGetDate = new List<SqlParameter> { new SqlParameter("@OldName", name) };
                DataTable dtDate = SqlDataHelper.ExecuteQuery(queryGetDate, paramsGetDate);

                DateTime originalDate = (DateTime)dtDate.Rows[0]["CreationDate"];  

                // Faz update do nome 
                string queryUpdate = "UPDATE Application SET Name = @NewName WHERE Name = @OldName";
                List<SqlParameter> paramsUpdate = new List<SqlParameter>
                {
                    new SqlParameter("@NewName", app.Name),
                    new SqlParameter("@OldName", name)
                };

                // 3. Preparamos o objeto final para devolver ao utilizador
                // Juntamos o nome novo com a data antiga!
                app.CreationDate = originalDate;
                SqlDataHelper.ExecuteNonQuery(queryUpdate, paramsUpdate);
                return Ok(app);
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627) return Conflict(); // Nome já existe
                return InternalServerError(ex);
            }
        }

        // ==========================================
        // APAGAR (DELETE): api/somiod/{name}
        // Remove a aplicação
        // ==========================================
        [HttpDelete]
        [Route("{name}")]
        public IHttpActionResult DeleteApplication(string name)
        {
            // Apaga a aplicação com este nome
            string query = "DELETE FROM Application WHERE Name = @Name";
            List<SqlParameter> parameters = new List<SqlParameter>();
            parameters.Add(new SqlParameter("@Name", name));

            int rowsAffected = SqlDataHelper.ExecuteNonQuery(query, parameters);

            // Se rowsAffected for 0, é porque não encontrou nada para apagar
            if (rowsAffected == 0) return NotFound();

            return Ok("Aplicação apagada com sucesso.");
        }

        //criar o objeto da class filho ou seja , criar os containers :D

        // ==========================================
        // CRIAR CONTAINER: POST api/somiod/{appName}
        // ==========================================
        [HttpPost]
        [Route("{appName}")]
        public IHttpActionResult CreateContainer(string appName, [FromBody] Container container)
        {
            if (container == null || string.IsNullOrEmpty(container.Name)) return BadRequest("Nome do container obrigatório.");
            if (container.ResType != "container") return BadRequest("Tipo incorreto. Esperado: container");

            // 1. Verificar se a Aplicação Pai existe e obter o ID dela
            string queryApp = "SELECT Id FROM Application WHERE Name = @AppName";
            List<SqlParameter> paramsApp = new List<SqlParameter> { new SqlParameter("@AppName", appName) };

            System.Data.DataTable dt = SqlDataHelper.ExecuteQuery(queryApp, paramsApp);

            if (dt.Rows.Count == 0) return NotFound(); // A App pai não existe!

            int parentId = (int)dt.Rows[0]["Id"]; 

            container.CreationDate = DateTime.Now;
            // 2. Criar o Container ligado a esse Pai
            try
            {
                string queryInsert = "INSERT INTO Container (Name, ParentAppId) VALUES (@Name, @ParentId)";
                List<SqlParameter> paramsInsert = new List<SqlParameter>
                {   
                    new SqlParameter("@Date",container.CreationDate),
                    new SqlParameter("@Name", container.Name),
                    new SqlParameter("@ParentId", parentId)
                };

                SqlDataHelper.ExecuteNonQuery(queryInsert, paramsInsert);

                return Ok(container);
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627) return Conflict(); // Nome duplicado
                return InternalServerError(ex);
            }
        }

    }

}