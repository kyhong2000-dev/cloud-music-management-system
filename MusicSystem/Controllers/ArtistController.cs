using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MusicSystem.Areas.Identity.Data;
using MusicSystem.Data;
using MusicSystem.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
using Amazon.S3.Model;
using Amazon.S3;
using Microsoft.AspNetCore.Http;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;

namespace MusicSystem.Controllers
{
    public class ArtistController : Controller
    {
        private readonly MusicSystemContext _context;
        private readonly UserManager<MusicSystemUser> _userManager;
        private const string bucketName = "songimageuploadbuckettp051305";
        private const string tableName = "wishlistTable";

        private IWebHostEnvironment _hostEnvironment;

        public ArtistController(IWebHostEnvironment hostarea,MusicSystemContext context,UserManager<MusicSystemUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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
            var user = await _userManager.GetUserAsync(User);
            ViewBag.ArtistStatus = user.ArtistStatus;
            if (user.ArtistStatus == "Verified")
            {
                var songs = from s in _context.Song select s;
                if (!String.IsNullOrEmpty(SearchString))
                    songs = songs.Where(s => s.songName.Contains(SearchString));
                ViewBag.ArtistName = _context.Artist.Where(x => x.UserID == user.Id).First().ArtistName;

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


                return View(await songs.ToListAsync());
            }
  
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "GeneralAccess")]
        public async Task<IActionResult> RegisterArtist([Bind("ArtistName,ArtistIC,ArtistContact")] Artist artist)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                user.ArtistStatus = "Pending";
                artist.UserID = user.Id;
                artist.ArtistStatus = "Pending";
                artist.TotalEarning = 0;
                _context.Add(artist);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return RedirectToAction(nameof(Index));
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

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "GeneralAccess")]
        public async Task<IActionResult> Create([Bind("songId,songName,duration,albumName,releasedDate,songCost,songFileName")] Song song, List<IFormFile> mp3Files)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                song.artistName = _context.Artist.Where(x => x.UserID == user.Id).First().ArtistName;
                _context.Add(song);
                await _context.SaveChangesAsync();

                string message = "";
                List<string> credentialInfo = getAWSCredentialInfo();
                var S3Client = new AmazonS3Client(credentialInfo[0], credentialInfo[1], credentialInfo[2], Amazon.RegionEndpoint.USEast1);
                foreach (var item in mp3Files)
                {
                    using (S3Client)
                    {
                        var songUploadRequest = new PutObjectRequest()
                        {
                            InputStream = item.OpenReadStream(),
                            BucketName = bucketName + "/mp3 Song File",
                            Key = item.FileName,
                            CannedACL = S3CannedACL.PublicRead,
                        };

                        PutObjectResponse result = await S3Client.PutObjectAsync(songUploadRequest);
                    }
                }
                message = "song file uploaded to S3";

                return RedirectToAction(nameof(Index));
            }
            return View(song);
        }


        [Authorize(Policy = "GeneralAccess")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();
            var song = await _context.Song.FindAsync(id);
            if (song == null)
                return NotFound();
            return View(song);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "GeneralAccess")]
        public async Task<IActionResult> Edit(int id, [Bind("songId,songName,artistName,duration,albumName,releasedDate,songCost")] Song song)
        {
            if (id != song.songId)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _userManager.GetUserAsync(User);
                    song.artistName = _context.Artist.Where(x => x.UserID == user.Id).First().ArtistName;
                    var songOld = await _context.Song.AsNoTracking().FirstOrDefaultAsync(x => x.songId == id);
                    song.songDownload = songOld.songDownload;
                    song.totalEarning = songOld.totalEarning;
                    _context.Update(song);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Song.Any(e => e.songId == id))
                        return NotFound();
                    else
                        throw;

                }
                return RedirectToAction(nameof(Index));
            }
            return View(song);
        }

        [Authorize(Policy = "GeneralAccess")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var song = await _context.Song.FirstOrDefaultAsync(m => m.songId == id);
            if (song == null)
                return NotFound();

            List<string> credentialInfo = getAWSCredentialInfo();
            var S3Client = new AmazonS3Client(credentialInfo[0], credentialInfo[1], credentialInfo[2], Amazon.RegionEndpoint.USEast1);

            var displayresult = new List<S3Object>();
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

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "GeneralAccess")]
        public async Task<IActionResult> DeleteConfirmed(int id,String FileName)
        {
            string message = "";
            var song = await _context.Song.FindAsync(id);
            _context.Song.Remove(song);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        
        public IActionResult ViewWishList(string msg = "")
        {
            ViewBag.msg = msg;
            return View();

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ViewWishList(string operators, string ArtistName)
        {
            string message = "";
            List<string> credentialInfo = getAWSCredentialInfo();
            var dynamoDbClient = new AmazonDynamoDBClient(credentialInfo[0], credentialInfo[1], credentialInfo[2], Amazon.RegionEndpoint.USEast1);

            List<Document> documentList = new List<Document>(); //dynamodbV2document model - use for reading from DB
            List<KeyValuePair<string, string>> singleSongRequestRecord = new List<KeyValuePair<string, string>>(); //use for frontend
            List<List<KeyValuePair<string, string>>> songRequestRecords = new List<List<KeyValuePair<string, string>>>();

            try
            {
                //step 1: create statement /process for full scan and filter(condition - optional) data in side the table
                ScanFilter scanArtistName = new ScanFilter();
                if (operators == "=")
                    scanArtistName.AddCondition("ArtistName", ScanOperator.Equal, Convert.ToString(ArtistName));

                //step 2: execute the commands
                Table customerTransactions = Table.LoadTable(dynamoDbClient, tableName);
                Search search = customerTransactions.Scan(scanArtistName);

                //step 3: loop to get everything one by one from the reading
                do
                {
                    documentList = await search.GetNextSetAsync();
                    if (documentList.Count == 0)
                    {
                        ViewBag.msg = "No Matching Song Request Submitted!";
                        return View();
                    }

                    foreach (var document in documentList) //read the single record keys and values 1 by 1
                    {
                        singleSongRequestRecord = GetValues(document); //GetValues - our own method - extract the key value one by one ans store in list (without using object oriented idea)
                        singleSongRequestRecord.Sort(Compare1); //Compare1 - also our own method - sort the key attriibute in an ascending order
                        songRequestRecords.Add(singleSongRequestRecord);
                    }

                } while (!search.IsDone);
                ViewBag.msg = "Song Request Found!";
            }
            catch (AmazonDynamoDBException ex)
            {
                ViewBag.msg = "Error: " + ex.Message;
            }
            catch (Exception ex)
            {
                ViewBag.msg = "Error: " + ex.Message;
            }

            return View(songRequestRecords);
        }

        public List<KeyValuePair<string, string>> GetValues(Document document) // read values and keys from single file
        {
            var records = new List<KeyValuePair<string, string>>(); //single record

            //extract the key and value from the document file
            foreach (var attribute in document.GetAttributeNames()) //attribute = key name
            {
                string stringValue = null;
                var value = document[attribute]; //value = document key value

                if (value is DynamoDBBool) //change the type from document type to real string type
                    stringValue = value.AsBoolean().ToString();
                else if (value is Primitive)
                    stringValue = value.AsPrimitive().Value.ToString();
                else if (value is PrimitiveList)
                    stringValue = string.Join(",", (from primitive
                                    in value.AsPrimitiveList().Entries
                                                    select primitive.Value).ToArray());
                records.Add(new KeyValuePair<string, string>(attribute, stringValue));
            }
            return records;
        }

        static int Compare1(KeyValuePair<string, string> a, KeyValuePair<string, string> b)
        {
            return a.Key.CompareTo(b.Key); //arrange the list based on key name in ascending order - a - z
        }


        //create function to delete the item in S3
        public async Task<IActionResult> deleteimage(string FileName)
        {
            string message = "";
            List<string> credentialInfo = getAWSCredentialInfo();
            var S3Client = new AmazonS3Client(credentialInfo[0], credentialInfo[1], credentialInfo[2], Amazon.RegionEndpoint.USEast1);

            try
            {
                if (string.IsNullOrEmpty(FileName)) //check error: is it the filename is empty?
                    return BadRequest("The " + FileName + " parameter is required");

                var deleteObjectRequest = new DeleteObjectRequest
                {
                    BucketName = bucketName,
                    Key = FileName
                };

                await S3Client.DeleteObjectAsync(deleteObjectRequest);
                message = FileName + " is deleted from the S3 now!";

            }
            catch (Exception ex)
            {
                message = FileName + " is not successfully deleted from the S3 bucket! \\n Error: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

    }
}
