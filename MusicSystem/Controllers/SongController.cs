using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MusicSystem.Areas.Identity.Data;
using MusicSystem.Data;
using MusicSystem.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace MusicSystem.Controllers
{
    public class SongController : Controller
    {
        private readonly MusicSystemContext _context;
        private readonly UserManager<MusicSystemUser> _userManager;
        private const string bucketName = "songimageuploadbuckettp051305";

        private IWebHostEnvironment _hostEnvironment;

        private const string tableName = "wishlistTable";

        public SongController(IWebHostEnvironment hostarea,MusicSystemContext context, UserManager<MusicSystemUser> UserManager)
        {
            _context = context;
            _userManager = UserManager;
            _hostEnvironment = hostarea;
        }

        private List<string> getAWSCredentialInfo()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");
            IConfigurationRoot configure = builder.Build();

            List<string> credentialInfo = new List<string>();
            credentialInfo.Add(configure["AWSCredential:accessKey"]);
            credentialInfo.Add(configure["AWSCredential:secretKey"]);
            credentialInfo.Add(configure["AWSCredential:SessionToken"]);
            return credentialInfo;
        }

        [Authorize(Policy = "GeneralAccess")]
        public async Task<IActionResult> Index(string SearchString, string ArtistName)
        {
            var songs = from s in _context.Song select s;

            if (!String.IsNullOrEmpty(SearchString))
                songs = songs.Where(s => s.songName.Contains(SearchString));

            IQueryable<String> TypeQuery = from s in _context.Song orderby s.artistName select s.artistName;
            IEnumerable<SelectListItem> items = new SelectList(await TypeQuery.Distinct().ToListAsync());
            ViewBag.ArtistName = items;

            if (!String.IsNullOrEmpty(ArtistName))
                songs = songs.Where(s => s.artistName.Equals(ArtistName));

            return View(await songs.ToListAsync());
        }

        [Authorize(Policy = "GeneralAccess")]
        public async Task<IActionResult> Detail(int? id)
        {
            if (id == null)
                return NotFound();
            
            var song = await _context.Song.FirstOrDefaultAsync(m => m.songId == id);
            if (song == null)
                return NotFound();
            
            return View(song);
        }

        [Authorize(Policy = "GeneralAccess")]
        public async Task<IActionResult> Purchase(int? id)
        {
            if (id == null)
                return NotFound();

            var song = await _context.Song.FirstOrDefaultAsync(m => m.songId == id);
            if (song == null)
                return NotFound();
            var user = await _userManager.GetUserAsync(User);
            if (user.AccountBalance < song.songCost)
                ViewBag.Error = true;

            /**Fetch all the song files from the S3 Bucket*/
            List<string> credentialInfo = getAWSCredentialInfo();
            var displayresult = new List<S3Object>();
            var S3Client = new AmazonS3Client(credentialInfo[0], credentialInfo[1], credentialInfo[2], Amazon.RegionEndpoint.USEast1);
            string token = null;
            List<string> presignedURLS = new List<string>(); //extra: add on the presigned listing

            try
            {
                do
                {
                    ListObjectsRequest viewrequest = new ListObjectsRequest()
                    {
                        BucketName = bucketName
                    };

                    ListObjectsResponse response = await S3Client.ListObjectsAsync(viewrequest).ConfigureAwait(false);
                    displayresult.AddRange(response.S3Objects); //keep all the object metadata to the list
                    token = response.NextMarker;
                }
                while (token != null); //to check when to stop reading from the s3

                //learn how to make a presigned url using the programmatically
                foreach (var item in displayresult)
                {
                    // Create a CopyObject request
                    GetPreSignedUrlRequest request = new GetPreSignedUrlRequest
                    {
                        BucketName = item.BucketName,
                        Key = item.Key,
                        Expires = DateTime.Now.AddMinutes(2)
                    };

                    // Get path for request
                    presignedURLS.Add(S3Client.GetPreSignedURL(request));
                }
                ViewBag.SongDownloadLinks = presignedURLS;

            }
            catch (Exception ex)
            {

            }

            ViewBag.SongFile = displayresult;

            return View(song);
        }

        [HttpPost, ActionName("Purchase")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "GeneralAccess")]
        public async Task<IActionResult> PurchaseConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var song = await _context.Song.FindAsync(id);
            user.AccountBalance = user.AccountBalance - song.songCost;
            user.AccountSpending = user.AccountSpending + song.songCost;
            var artist = _context.Artist.Where(x => x.ArtistName == song.artistName).FirstOrDefault();
            artist.TotalEarning = artist.TotalEarning + song.songCost;
            var artistUser = _userManager.Users.Where(x => x.Id == artist.UserID).FirstOrDefault();
            artistUser.AccountBalance = artistUser.AccountBalance + song.songCost;
            song.songDownload = song.songDownload + 1;
            song.totalEarning = song.totalEarning + song.songCost;
            TempData["Purchase"] = "Success";
            await _context.SaveChangesAsync();

            return RedirectToAction("Purchase");
        }

        public IActionResult Wishlist(string msg = "")
        {
            ViewBag.msg = msg;
            return View();
        }

        public async Task<IActionResult> CreateWishlistTable()
        {
            string message = "";
            List<string> credentialInfo = getAWSCredentialInfo();
            var dynamoDbClient = new AmazonDynamoDBClient(credentialInfo[0], credentialInfo[1], credentialInfo[2], Amazon.RegionEndpoint.USEast1);

            try //create schema info of the table
            {
                var tableRequest = new CreateTableRequest
                {
                    //table setting
                    TableName = tableName,
                    AttributeDefinitions = new List<AttributeDefinition>()
                    {
                        new AttributeDefinition //partition key
                        {
                            AttributeName = "SongWishlistID",
                            AttributeType = "S"
                        },
                        new AttributeDefinition //sort key
                        {
                            AttributeName = "ArtistName",
                            AttributeType = "S"
                        }
                    },
                    KeySchema = new List<KeySchemaElement>()
                    {
                        new KeySchemaElement //partition key
                        {
                            AttributeName = "SongWishlistID",
                            KeyType = "HASH"
                        },
                        new KeySchemaElement //sort key
                        {
                            AttributeName = "ArtistName",
                            KeyType = "RANGE"
                        }
                    },
                    ProvisionedThroughput = new ProvisionedThroughput
                    {
                        ReadCapacityUnits = 5,
                        WriteCapacityUnits = 10
                    }
                };

                await dynamoDbClient.CreateTableAsync(tableRequest);
                message = tableName + " is created now!";
            }
            catch (Exception ex)
            {
                message = "Table unable to create. Error as below: \\n" + ex.Message;
            }
            return RedirectToAction("Wishlist", "Song", new { msg = message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> addSongWishlistData(string songWishlistID, string songRequestName, string songArtistName, string songRequestDate)
        {
            string message = "";
            List<string> credentialInfo = getAWSCredentialInfo();
            var dynamoDbClient = new AmazonDynamoDBClient(credentialInfo[0], credentialInfo[1], credentialInfo[2], Amazon.RegionEndpoint.USEast1);
            Dictionary<string, AttributeValue> attributes = new Dictionary<string, AttributeValue>();
            try
            {
                //CustomerID - Partition Key, TransactionID - Sort Key (Unique to that particular group)
                attributes["SongWishlistID"] = new AttributeValue { S = songWishlistID };
                attributes["ArtistName"] = new AttributeValue { S = songArtistName.ToString() };
                attributes["songRequestName"] = new AttributeValue { S = songRequestName.ToString() };
                attributes["songRequestDate"] = new AttributeValue { S = songRequestDate.ToString() };


                PutItemRequest putRequest = new PutItemRequest
                {
                    TableName = tableName,
                    Item = attributes
                };

                await dynamoDbClient.PutItemAsync(putRequest);
                message = "The song request with the name of " + songRequestName + " is successfully made! Thank you for your suggestion!";
            }
            catch (AmazonDynamoDBException ex)
            {
                message = "Error: " + ex.Message;
            }
            catch (Exception ex)
            {
                message = "Error: " + ex.Message;
            }
            return RedirectToAction("Wishlist", "Song", new { msg = message });
        }
    }
}
