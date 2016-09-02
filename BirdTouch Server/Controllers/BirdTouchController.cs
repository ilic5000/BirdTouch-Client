using BirdTouch_Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Globalization;

namespace BirdTouch_Server.Controllers
{
    public class BirdTouchController : ApiController
    {

        //NOTE:
        //razvijao sam aplikaciju koristeci MYSQL bazu, ali zbog glupavih connectora
        //MORAO SAM da predjem na sql server, za sada sam prezadovoljan
        //entityframework gotovo da nije osetio promenu, samo sam promenio ime nekih klasa. fenomenalno.

        [HttpGet]
        [Route("rest/getUser/{id}")] //test, ne koristi se nigde u aplikaciji, prvi rest servis koji sam napisao u c#, sentimentalno sam vezan
        public IHttpActionResult getUser(int id)
        {
            using (var context = new EntityFrameworkModels.birdtouchEntities2())
            {

                var ResultSetUserInfo = (from s in context.users
                                         join sa in context.user_info on s.id equals sa.id_user_private
                                         where sa.id_user_private == id
                                         select new User()
                                         {
                                             Id = s.id,
                                             Username = s.username,
                                             FirstName = sa.firstName,
                                             LastName = sa.lastName,
                                             Email = sa.email,
                                             PhoneNumber = sa.phoneNumber,
                                             DateOfBirth = sa.dateOfBirth,
                                             Adress = sa.adress,
                                             ProfilePictureData = sa.profilePictureData,
                                             FbLink = sa.fbLink,
                                             TwitterLink = sa.twLink,
                                             GPlusLink = sa.gPlusLink,
                                             LinkedInLink = sa.linkedInLink
                                         });

                var userInfo = ResultSetUserInfo.FirstOrDefault<User>();
                if (userInfo != null) return Ok(userInfo);
            }

            return NotFound();
        }


        [HttpGet]
        [Route("rest/getUserLogin")]
        public IHttpActionResult getUserLogin()
        {

            IEnumerable<string> headerValues;

            headerValues = Request.Headers.GetValues("username");
            String username = headerValues.FirstOrDefault();


            headerValues = Request.Headers.GetValues("password");
            String password = headerValues.FirstOrDefault();

            using (var context = new EntityFrameworkModels.birdtouchEntities2())
            {

                var ResultSetUserInfo = (from s in context.users
                                         join sa in context.user_info on s.id equals sa.id_user_private
                                         where s.username == username && s.password == password
                                         select new User()
                                         {
                                             Id = s.id,
                                             Username = s.username,
                                             FirstName = sa.firstName,
                                             LastName = sa.lastName,
                                             Email = sa.email,
                                             PhoneNumber = sa.phoneNumber,
                                             DateOfBirth = sa.dateOfBirth,
                                             Adress = sa.adress,
                                             ProfilePictureData = sa.profilePictureData,
                                             FbLink = sa.fbLink,
                                             TwitterLink = sa.twLink,
                                             GPlusLink = sa.gPlusLink,
                                             LinkedInLink = sa.linkedInLink
                                         });

                var userInfo = ResultSetUserInfo.FirstOrDefault<User>();
                if (userInfo != null) { 
                    UserEncodedImage userEncoded = new UserEncodedImage() //novaKlasa zbog base64string konverzije niza bajtova 
                {
                    Adress = userInfo.Adress,
                    DateOfBirth = userInfo.DateOfBirth,
                    Email = userInfo.Email,
                    FbLink = userInfo.FbLink,
                    FirstName = userInfo.FirstName,
                    Username = userInfo.Username,
                    GPlusLink = userInfo.GPlusLink,
                    Id = userInfo.Id,
                    LastName = userInfo.LastName,
                    LinkedInLink = userInfo.LinkedInLink,
                    PhoneNumber = userInfo.PhoneNumber,
                    TwitterLink = userInfo.TwitterLink,
                };

                if(userInfo.ProfilePictureData != null)
                {
                    userEncoded.ProfilePictureDataEncoded = Convert.ToBase64String(userInfo.ProfilePictureData);
                }              
                 return Ok(userEncoded); //  String serialized = Newtonsoft.Json.JsonConvert.SerializeObject(userEncoded);
                }

            }

            return NotFound();
        }

        [HttpGet]
        [Route("rest/doesUserExist")]
        public IHttpActionResult doesUserExist()
        {

            IEnumerable<string> headerValues;

            headerValues = Request.Headers.GetValues("username");
            String username = headerValues.FirstOrDefault();


            headerValues = Request.Headers.GetValues("password");
            String password = headerValues.FirstOrDefault();

            using (var context = new EntityFrameworkModels.birdtouchEntities2())
            {

                var ResultSetUserInfo = (from s in context.users

                                        where s.username == username && s.password == password
                                        select new SimpleUser()
                                        {
                                            Id = s.id,
                                            Username = s.username,
                                            Password = s.password,
                                            
                                        });

                var userInfo = ResultSetUserInfo.FirstOrDefault<SimpleUser>();
                if (userInfo != null) return Ok(userInfo);
            }

            return NotFound();
        }


        [HttpGet] 
        [Route("rest/doesUsernameExist")]
        public IHttpActionResult doesUsernameExist()
        {

            IEnumerable<string> headerValues;

            headerValues = Request.Headers.GetValues("username");
            String username = headerValues.FirstOrDefault();


            using ( var context = new EntityFrameworkModels.birdtouchEntities2())
            {

                var ResultSetUserInfo = (from s in context.users

                                        where s.username == username
                                        select new SimpleUser()
                                        {
                                            Id = s.id,
                                            Username = s.username,
                                            Password = s.password,
                                            
                                        });

                var userInfo = ResultSetUserInfo.FirstOrDefault<SimpleUser>();
                if (userInfo != null) return Ok(userInfo);

            }

            return NotFound();
        }


        [HttpGet]
        [Route("rest/registerUser")]
        public IHttpActionResult registerUser()
        {


            IEnumerable<string> headerValues;

            headerValues = Request.Headers.GetValues("username");
            String username2 = headerValues.FirstOrDefault();
            

            headerValues = Request.Headers.GetValues("password");
            String password2 = headerValues.FirstOrDefault();


            using (var context = new EntityFrameworkModels.birdtouchEntities2())
            {

                context.users.Add(new EntityFrameworkModels.users()
                {
                    
                    username = username2,
                    password = password2
                    

                });


          
                context.user_info.Add(new EntityFrameworkModels.user_info());
                context.business_info.Add(new EntityFrameworkModels.business_info());
                context.SaveChanges();

            

                 var ResultSetUserInfo2 = (from s in context.users
                                         join sa in context.user_info on s.id equals sa.id_user_private
                                         where s.username == username2 && s.password == password2
                                         select new User()
                                         {
                                             Id = s.id,
                                             Username = s.username,
                                             FirstName = sa.firstName,
                                             LastName = sa.lastName,
                                             Email = sa.email,
                                             PhoneNumber = sa.phoneNumber,
                                             DateOfBirth = sa.dateOfBirth,
                                             Adress = sa.adress,
                                             ProfilePictureData = sa.profilePictureData,
                                             FbLink = sa.fbLink,
                                             TwitterLink = sa.twLink,
                                             GPlusLink = sa.gPlusLink,
                                             LinkedInLink = sa.linkedInLink
                                         });

                var userInfo2 = ResultSetUserInfo2.FirstOrDefault<User>();
                if (userInfo2 != null) return Ok(userInfo2); //mozda treba da se smisli nekako da ne vraca ovaj objekat, ali vec sam napravio da Register i startPage u klijentu ovako radi, sa ovim objektom, zato ga vracam
            }

            return NotFound();
        }


        [HttpPost]
        [Route("rest/changeUserPrivateInfo")]
        public IHttpActionResult changeUserPrivateInfo()

        {
            IEnumerable<string> headerValues;

            headerValues = Request.Headers.GetValues("id");
            String id = headerValues.FirstOrDefault();
            int id2 = Int32.Parse(id);

            headerValues = Request.Headers.GetValues("firstname");
            String firstname2 = headerValues.FirstOrDefault();


            headerValues = Request.Headers.GetValues("lastname");
            String lastname2 = headerValues.FirstOrDefault();

            headerValues = Request.Headers.GetValues("email");
            String email2 = headerValues.FirstOrDefault();


            headerValues = Request.Headers.GetValues("phone");
            String phone2 = headerValues.FirstOrDefault();

            headerValues = Request.Headers.GetValues("adress");
            String adress2 = headerValues.FirstOrDefault();

            headerValues = Request.Headers.GetValues("dateofbirth");
            String dateofbirth2 = headerValues.FirstOrDefault();

            headerValues = Request.Headers.GetValues("fblink");
            String fblink2 = headerValues.FirstOrDefault();

            headerValues = Request.Headers.GetValues("twlink");
            String twitterlink2 = headerValues.FirstOrDefault();

            headerValues = Request.Headers.GetValues("gpluslink");
            String gpluslink2 = headerValues.FirstOrDefault();

            headerValues = Request.Headers.GetValues("linkedinlink");
            String linkedin2 = headerValues.FirstOrDefault();

            byte[] profilePicDataTransfered = Request.Content.ReadAsByteArrayAsync().Result;

            //jedini nacin za sada da ovo radi, mozda nekad pronadjem bolji nacin
            if (firstname2.Equals("NULL")) firstname2 = null;
            if (lastname2.Equals("NULL")) lastname2 = null;
            if (email2.Equals("NULL")) email2 = null;
            if (phone2.Equals("NULL")) phone2 = null;
            if (adress2.Equals("NULL")) adress2 = null;
            if (dateofbirth2.Equals("NULL")) dateofbirth2 = null;
            if (fblink2.Equals("NULL")) fblink2 = null;
            if (twitterlink2.Equals("NULL")) twitterlink2 = null;
            if (gpluslink2.Equals("NULL")) gpluslink2 = null;
            if (linkedin2.Equals("NULL")) linkedin2 = null;
            

            using (var context = new EntityFrameworkModels.birdtouchEntities2()) //dvojka jer sam presao na sqlserver
            {


                EntityFrameworkModels.user_info result = context.user_info.SingleOrDefault<EntityFrameworkModels.user_info>(b => b.id_user_private == id2);
                if (result != null)
                {

                    result.firstName = firstname2;
                    result.lastName = lastname2;
                    result.email = email2;
                    result.phoneNumber = phone2;
                    result.adress = adress2;
                    result.dateOfBirth = dateofbirth2;
                    result.fbLink = fblink2;
                    result.twLink = twitterlink2;
                    result.gPlusLink = gpluslink2;
                    result.linkedInLink = linkedin2;

                    if (profilePicDataTransfered.Length != 1)
                        result.profilePictureData = profilePicDataTransfered;

                    context.SaveChanges();
                    return Ok("User data changed succesfully");
                }
                else
                {
                    return NotFound();
                }

            }


        }




        [HttpGet]
        [Route("rest/makeUserVisible")]
        public IHttpActionResult makeUserVisible()

        {
            IEnumerable<string> headerValues;

            headerValues = Request.Headers.GetValues("id");
            String id = headerValues.FirstOrDefault();
            int id2 = Int32.Parse(id);

            headerValues = Request.Headers.GetValues("longitude");
            String longitudeString = headerValues.FirstOrDefault();
            double longitude = Double.Parse(longitudeString, CultureInfo.InvariantCulture);

            headerValues = Request.Headers.GetValues("latitude");
            String latitudeString = headerValues.FirstOrDefault();
            double latitude = Double.Parse(latitudeString, CultureInfo.InvariantCulture);

            headerValues = Request.Headers.GetValues("mode");
            String modeString = headerValues.FirstOrDefault();
            int mode = Int32.Parse(modeString);



            using (var context = new EntityFrameworkModels.birdtouchEntities2())
            {


                var userExistInThisMode = context.active_users.SingleOrDefault(x => x.user_id == id2 && x.active_mode==mode);

                if (userExistInThisMode == null)

                {
                    context.active_users.Add(new EntityFrameworkModels.active_users
                    {

                        active_mode = mode,
                        location_latitude = (decimal)latitude,
                        location_longitude = (decimal)longitude,
                        user_id = id2
                    });

                    
                 }
                else
                {
                    //ako je vec ulogovan, samo updatujemo lokaciju
                    userExistInThisMode.location_latitude = (decimal)latitude;
                    userExistInThisMode.location_longitude = (decimal)longitude;

                }

                context.SaveChanges();

            }

            return Ok();


        }



        [HttpGet]
        [Route("rest/makeUserInvisible")]
        public IHttpActionResult makeUserInvisible()

        {
            IEnumerable<string> headerValues;

            headerValues = Request.Headers.GetValues("id");
            String id = headerValues.FirstOrDefault();
            int id2 = Int32.Parse(id);

            headerValues = Request.Headers.GetValues("mode"); //za modove tek treba da se vidi logika
            String modeString = headerValues.FirstOrDefault();
            int mode = Int32.Parse(modeString);



            using (var context = new EntityFrameworkModels.birdtouchEntities2())
            {
               
                var itemToRemove = context.active_users.SingleOrDefault(x => x.user_id==id2 && x.active_mode == mode);

                //logika kako obraditi mode, da se oduzima pa ako je 0 da se brise, ili tako nesto

                if (itemToRemove != null)
                {
                    context.active_users.Remove(itemToRemove);
                    context.SaveChanges();
                    

                }
           

            }

            return Ok();


        }



        [HttpGet]
        [Route("rest/getBusiness/{idOwner}")] //public REST servis, ako neko bas hoce, moze i iz browsera da pretrazuje kompanije, visak vidljivosti njima ne smeta

        public IHttpActionResult getBusiness(int idOwner)
        {

            using (var context = new EntityFrameworkModels.birdtouchEntities2())
            {

                var ResultSetBusinessInfo = (from s in context.business_info
                                         
                                         where s.id_business_owner == idOwner
                                         select new Business()
                                         {
                                            Adress = s.adress,
                                            CompanyName = s.companyname,
                                            Email = s.email,
                                            IdBusinessOwner = s.id_business_owner,
                                            PhoneNumber = s.phonenumber,
                                            Website = s.website,
                                            ProfilePictureData = s.profilepicturedata
                                         });

                var businessInfo = ResultSetBusinessInfo.FirstOrDefault<Business>();

                if (businessInfo != null) { 
                    BusinessEncoded businessEncoded = new BusinessEncoded()
                {
                    Website = businessInfo.Website,
                    Adress = businessInfo.Adress,
                    CompanyName = businessInfo.CompanyName,
                    Email = businessInfo.Email,
                    IdBusinessOwner = businessInfo.IdBusinessOwner,
                    PhoneNumber = businessInfo.PhoneNumber,
                    

                };

                if(businessInfo.ProfilePictureData != null)
                {
                    businessEncoded.ProfilePictureDataEncoded = Convert.ToBase64String(businessInfo.ProfilePictureData);
                }
                
                return Ok(businessEncoded);//  String serialized = Newtonsoft.Json.JsonConvert.SerializeObject(userEncoded);
                }

            }

            return NotFound();
        }


        [HttpPost]
        [Route("rest/changeUserBusinessInfo")]
        public IHttpActionResult changeUserBusinessInfo()

        {
            IEnumerable<string> headerValues;

            headerValues = Request.Headers.GetValues("id");
            String id = headerValues.FirstOrDefault();
            int id2 = Int32.Parse(id);

            headerValues = Request.Headers.GetValues("companyname");
            String companyname2 = headerValues.FirstOrDefault();

            headerValues = Request.Headers.GetValues("email");
            String email2 = headerValues.FirstOrDefault();

            headerValues = Request.Headers.GetValues("phone");
            String phone2 = headerValues.FirstOrDefault();

            headerValues = Request.Headers.GetValues("adress");
            String adress2 = headerValues.FirstOrDefault();

            headerValues = Request.Headers.GetValues("website");
            String website2 = headerValues.FirstOrDefault();


            byte[] profilePicDataTransfered = Request.Content.ReadAsByteArrayAsync().Result;

            //jedini nacin za sada da ovo radi, mozda nekad pronadjem bolji nacin
            if (companyname2.Equals("NULL")) companyname2 = null;
            if (email2.Equals("NULL")) email2 = null;
            if (phone2.Equals("NULL")) phone2 = null;
            if (adress2.Equals("NULL")) adress2 = null;
            if (website2.Equals("NULL")) website2 = null;
           


            using (var context = new EntityFrameworkModels.birdtouchEntities2()) //dvojka jer sam presao na sqlserver
            {


                EntityFrameworkModels.business_info result = context.business_info.SingleOrDefault<EntityFrameworkModels.business_info>(b => b.id_business_owner == id2);
                if (result != null)
                {

                    result.companyname = companyname2;
                    result.website = website2;
                    result.email = email2;
                    result.phonenumber = phone2;
                    result.adress = adress2; 
                      
                    if(profilePicDataTransfered.Length != 1)
                        result.profilepicturedata = profilePicDataTransfered;

                    context.SaveChanges();
                    return Ok("Business data changed succesfully");
                }
                else
                {
                    return NotFound();
                }

            }


        }


        [HttpGet]
        [Route("rest/getPrivateUsersNearMe")]
        public IHttpActionResult getPrivateUsersNearMe()

        {
            IEnumerable<string> headerValues;

            headerValues = Request.Headers.GetValues("id");
            String id = headerValues.FirstOrDefault();
            int id5 = Int32.Parse(id);


            //mozda da se napravi jos jedan parametar za radijus

            decimal? mineLongitude = 0;
            decimal? mineLatitude = 0;


            using (var context = new EntityFrameworkModels.birdtouchEntities2())
            {

                var me = context.active_users.SingleOrDefault(x => x.user_id == id5 && x.active_mode == 1);
                mineLatitude = me.location_latitude;
                mineLongitude = me.location_longitude;

                //sada imamo moju lokaciju iz baze


                List<int?> listOfUsersIdAroundMe = new List<int?>();


                foreach (var active_user in context.active_users)
                {
                    if (active_user.user_id != id5 && active_user.active_mode == 1) { //da ne uporedjujemo sa samim sobom
                        double distance = new Coordinates((double)mineLatitude, (double)mineLongitude)
                    .DistanceTo(
                        new Coordinates((double)active_user.location_latitude, (double)active_user.location_longitude),
                        UnitOfLength.Kilometers
                    );

                        if (distance < 1.5) listOfUsersIdAroundMe.Add(active_user.user_id);


                    }
                }



                var tempResult = context.user_info.Where(x => listOfUsersIdAroundMe.Contains(x.id_user_private)).ToList<EntityFrameworkModels.user_info>();


                //ovaj deo koji sledi mozda nije potreban, treba ispitati da li moze bez ovog encoded dela, ali ovako je jasnije
                //takodje, username ostaje null zbog zastite

                List<UserEncodedImage> result = new List<UserEncodedImage>();

                foreach (var item in tempResult)
                {
                    UserEncodedImage userEncoded = new UserEncodedImage()
                    {
                        Adress = item.adress,
                        DateOfBirth = item.dateOfBirth,
                        Email = item.email,
                        FbLink = item.fbLink,
                        FirstName = item.firstName,
                        GPlusLink = item.gPlusLink,
                        Id = item.id_user_private,
                        LastName = item.lastName,
                        LinkedInLink = item.linkedInLink,
                        PhoneNumber = item.phoneNumber,
                        TwitterLink = item.twLink

                    };
                    if (item.profilePictureData != null)
                    {
                        userEncoded.ProfilePictureDataEncoded = Convert.ToBase64String(item.profilePictureData);
                    }
                    
                    
                    result.Add(userEncoded);

                }

                return Ok(result);

        }
        }

        [HttpGet]
        [Route("rest/getBusinessUsersNearMe")]
        public IHttpActionResult getBusinessUsersNearMe()

        {
            IEnumerable<string> headerValues;

            headerValues = Request.Headers.GetValues("id");
            String id = headerValues.FirstOrDefault();
            int id5 = Int32.Parse(id);


            //mozda da se napravi jos jedan parametar za radijus

            decimal? mineLongitude = 0;
            decimal? mineLatitude = 0;


            using (var context = new EntityFrameworkModels.birdtouchEntities2())
            {

                var me = context.active_users.SingleOrDefault(x => x.user_id == id5 && x.active_mode == 2);
                mineLatitude = me.location_latitude;
                mineLongitude = me.location_longitude;

                //sada imamo moju lokaciju iz baze


                List<int?> listOfUsersIdAroundMe = new List<int?>();


                foreach (var active_user in context.active_users)
                {
                    if (active_user.user_id != id5 && active_user.active_mode == 2)
                    { //da ne uporedjujemo sa samim sobom
                        double distance = new Coordinates((double)mineLatitude, (double)mineLongitude)
                    .DistanceTo(
                        new Coordinates((double)active_user.location_latitude, (double)active_user.location_longitude),
                        UnitOfLength.Kilometers
                    );

                        if (distance < 1.5) listOfUsersIdAroundMe.Add(active_user.user_id);


                    }
                }



                var tempResult = context.business_info.Where(x => listOfUsersIdAroundMe.Contains(x.id_business_owner)).ToList<EntityFrameworkModels.business_info>();


                //ovaj deo koji sledi mozda nije potreban, treba ispitati da li moze bez ovog encoded dela, ali ovako je jasnije
                //takodje username ostaje null zbog zastite

                List<BusinessEncoded> result = new List<BusinessEncoded>();

                foreach (var item in tempResult)
                {
                    BusinessEncoded businessEncoded = new BusinessEncoded()
                    {
                        CompanyName = item.companyname,
                        IdBusinessOwner = item.id_business_owner,
                        Website = item.website,
                        Adress = item.adress,     
                        Email = item.email,                        
                        PhoneNumber = item.phonenumber                       
                    };
                    if (item.profilepicturedata != null)
                    {
                        businessEncoded.ProfilePictureDataEncoded = Convert.ToBase64String(item.profilepicturedata);
                    }
                    

                    result.Add(businessEncoded);

                }

                return Ok(result);

            }
        }
















        //[HttpGet] //id2 mora jer vec posotji id u projektu, ne pitajte kako sam naso taj bug
        //[Route("rest/changeUserPrivateInfo/{id2}/{firstname2?}/{lastname2?}/{email2?}/{phone2?}/{adress2?}/{dateofbirth2?}/{fblink2?}/{twitterlink2?}/{gpluslink2?}/{linkedin2?}")]
        //public IHttpActionResult changeUserPrivateInfo(int id2, String firstname2 = null, String lastname2 = null, String email2 = null, String phone2 = null, String adress2 = null, String dateofbirth2 = null, String fblink2 = null, String twitterlink2 = null, String gpluslink2 = null, String linkedin2 = null)

        //{
        //    IEnumerable<string> headerValues;
        //    var picDataEncoded = string.Empty;

        //    if (Request.Headers.TryGetValues("picDataEncoded", out headerValues))
        //    {
        //        picDataEncoded = headerValues.FirstOrDefault();


        //            }


        //    byte[] picDataDecoded = Convert.FromBase64String(picDataEncoded);

        //    //jedini nacin za sada da ovo radi, mozda nekad pronadjem bolji nacin
        //    if (firstname2.Equals("NULL")) firstname2 = null;
        //    if (lastname2.Equals("NULL")) lastname2 = null;
        //    if (email2.Equals("NULL")) email2 = null;
        //    if (phone2.Equals("NULL")) phone2 = null;
        //    if (adress2.Equals("NULL")) adress2 = null;
        //    if (dateofbirth2.Equals("NULL")) dateofbirth2 = null;
        //    if (fblink2.Equals("NULL")) fblink2 = null;
        //    if (twitterlink2.Equals("NULL")) twitterlink2 = null;
        //    if (gpluslink2.Equals("NULL")) gpluslink2 = null;
        //    if (linkedin2.Equals("NULL")) linkedin2 = null;
        ////    if (profilepicturedata2.Equals("NULL")) linkedin2 = null;

        //    using (var context = new EntityFrameworkModels.birdtouchEntities2()) //dvojka jer sam presao na sqlserver
        //    {


        //        EntityFrameworkModels.user_info result = context.user_info.SingleOrDefault<EntityFrameworkModels.user_info>(b => b.id_user_private == id2);
        //        if (result != null)
        //        {

        //            result.firstName = firstname2;
        //            result.lastName = lastname2;
        //            result.email = email2;
        //            result.phoneNumber = phone2;
        //            result.adress = adress2;
        //            result.dateOfBirth = dateofbirth2;
        //            result.fbLink = fblink2;
        //            result.twLink = twitterlink2;
        //            result.gPlusLink = gpluslink2;
        //            result.linkedInLink = linkedin2;
        //            result.profilePictureData = picDataDecoded;
        //            context.SaveChanges();
        //            return Ok("User data changed succesfully");
        //        }
        //        else
        //        {
        //            return NotFound();
        //        }

        //    }


        //}



    }
}


//var ResultSetUserInfo = (from s in context.users

//                         where s.username == username2
//                         select new SimpleUser()
//                         {
//                             Id = s.id,
//                             Username = s.username,
//                             Password = s.password,

//                         });

//var justRegisterddUserInfo = ResultSetUserInfo.FirstOrDefault<SimpleUser>();
