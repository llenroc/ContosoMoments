﻿using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Cors;
using System.Web.Http.OData;
using ContosoMoments.Common.Models;
using Microsoft.Azure.Mobile.Server;
using System.Configuration;
using System;
using System.Diagnostics;
using System.Net.Http;
using ContosoMoments.API;
using ContosoMoments.API.Helpers;

namespace ContosoMoments.MobileServer.Controllers.TableControllers
{
    public class AlbumController : TableController<Album>
    {
        private MobileServiceContext dbContext;

        protected override void Initialize(HttpControllerContext controllerContext)
        {
            base.Initialize(controllerContext);

            dbContext = new MobileServiceContext();
            controllerContext.Configuration.Formatters.JsonFormatter.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            DomainManager = new EntityDomainManager<Album>(dbContext, Request, enableSoftDelete: IsSoftDeleteEnabled());
        }

        [EnableCors(origins: "*", headers: "*", methods: "*")]
        public IQueryable<Album> GetAllAlbum()
        {
            return Query();
        }

        [EnableCors(origins: "*", headers: "*", methods: "*")]
        [Route("tables/Album/{id}")]
        public SingleResult<Album> GetAlbum(string id)
        {
            return Lookup(id);
        }

        [EnableCors(origins: "*", headers: "*", methods: "*")]
        [Authorize]
        [Route("tables/Album/{id}")]
        public Task<Album> PatchAlbum(string id, Delta<Album> patch)
        {
            return UpdateAsync(id, patch);
        }

        [EnableCors(origins: "*", headers: "*", methods: "*")]
        [Authorize]
        [Route("tables/Album")]
        public async Task<IHttpActionResult> PostAlbum(Album item)
        {
            Album current = await InsertAsync(item);
            return CreatedAtRoute("Tables", new { id = current.Id }, current);
        }

        [EnableCors(origins: "*", headers: "*", methods: "*")]
        [Authorize]
        [Route("tables/Album/{albumId}")]
        public async Task DeleteAlbum(string albumId)
        {
            if (albumId == new ConfigModel().DefaultAlbumId) {
                var message =
                    new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest) { ReasonPhrase = "Cannot delete default album" };
                throw new HttpResponseException(message);
            }

            var album = await dbContext.Albums.FindAsync(albumId);

            var domainManager = new EntityDomainManager<Image>(dbContext, Request, IsSoftDeleteEnabled());

            foreach (var img in album.Images) {
                await domainManager.DeleteAsync(img.Id);
                await ImageController.DeleteBlobAsync(img.Id);
            }

            await DeleteAsync(albumId);
        }

        public static bool IsSoftDeleteEnabled()
        {
            return Convert.ToBoolean(ConfigurationManager.AppSettings["enableSoftDelete"]);
        }
    }
}