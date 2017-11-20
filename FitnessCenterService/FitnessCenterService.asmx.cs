using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web.Services;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Script.Services;
using FitnessCenterService.Models;
using FitnessCenterService.Utils;

namespace FitnessCenterService
{
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    public class FitnessCenterService : WebService
    {
        private readonly FitnessCenterEntities _db;

        public FitnessCenterService()
        {
            _db = new FitnessCenterEntities();
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json, UseHttpGet = true)]
        public void SearchClients(string firstName, string lastName, string email, int ssoId)
        {
            if (!ValidateUser(ssoId))
            {
                throw new Exception(ResourceStrings.SsoExpiredMessage);
            }
            List<Client> clients;
            if (string.IsNullOrEmpty(firstName) && string.IsNullOrEmpty(lastName) && string.IsNullOrEmpty(email))
            {
                clients = new List<Client>();
            }
            else
            {
                clients = _db.Clients.Where(client =>
                        (string.IsNullOrEmpty(firstName) ||
                         client.FirstName.ToLower().StartsWith(firstName.ToLower())) &&
                        (string.IsNullOrEmpty(lastName) || client.LastName.ToLower().StartsWith(lastName.ToLower())) &&
                        (string.IsNullOrEmpty(email) || client.FirstName.ToLower().StartsWith(email.ToLower()))
                ).ToList();
            }
            string result = JsonUtility.ObjectToJson(clients);
            WriteResponce(result);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json, UseHttpGet = true)]
        public void GetClient(int clientId, int ssoId)
        {
            if (!ValidateUser(ssoId))
            {
                throw new Exception(ResourceStrings.SsoExpiredMessage);
            }
            Client client = _db.Clients.FirstOrDefault(item => item.ClientId.Equals(clientId));
            string result = client != null ? JsonUtility.ObjectToJson(client) : ResourceStrings.ClientNotExistMessage;
            WriteResponce(result);
        }

        [WebMethod]
        [Roles(new [] { Roles.Admin })]
        [ScriptMethod(UseHttpGet = true)]
        public void UpdateClient(string clientJson, int ssoId)
        {
            Client client = JsonUtility.JsonToObject<Client>(clientJson);
            if (!ValidateUser(ssoId))
            {
                throw new Exception(ResourceStrings.SsoExpiredMessage);
            }
            Client currentClient = _db.Clients.FirstOrDefault(item => item.ClientId.Equals(client.ClientId));
            if (currentClient != null)
            {
                currentClient.FirstName = string.IsNullOrEmpty(client.FirstName)
                    ? currentClient.FirstName
                    : client.FirstName;
                currentClient.LastName = string.IsNullOrEmpty(client.LastName)
                    ? currentClient.LastName
                    : client.LastName;
                currentClient.Email = client.Email;
                currentClient.BirthDate = client.BirthDate ?? currentClient.BirthDate;
                _db.SaveChanges();
            }
        }

        [WebMethod]
        [Roles(new[] { Roles.Admin })]
        [ScriptMethod(UseHttpGet = true)]
        public void DeleteClient(int clientId, int ssoId)
        {
            string result = string.Empty;
            if (!ValidateUser(ssoId))
            {
                throw new Exception(ResourceStrings.SsoExpiredMessage);
            }
            Client client = _db.Clients.FirstOrDefault(item => item.ClientId.Equals(clientId));
            if (client != null)
            {
                _db.Clients.Remove(client);
                _db.SaveChanges();
            }
            else
            {
                result = ResourceStrings.ClientNotExistMessage;
            }
            WriteResponce(result);
        }

        [WebMethod]
        [Roles(new[] { Roles.Admin })]
        [ScriptMethod(UseHttpGet = true)]
        public void CreateClient(string clientJson, int ssoId)
        {
            Client client = JsonUtility.JsonToObject<Client>(clientJson);
            client.MemberSince = DateTime.Now;
            if (!ValidateUser(ssoId))
            {
                throw new Exception(ResourceStrings.SsoExpiredMessage);
            }
            _db.Clients.Add(client);
            _db.SaveChanges();
            string res = JsonUtility.ObjectToJson(client);
            WriteResponce(res);
        }

        [WebMethod]
        [Roles(new[] { Roles.Admin })]
        [ScriptMethod(UseHttpGet = true)]
        public void CreateUser(string email, string password, int ssoId)
        {
            User user = new User
            {
                Email = email,
                PasswordHash = password.GetHashCode(),
                IsEnabled = true
            };
            if (!ValidateUser(ssoId))
            {
                throw new Exception(ResourceStrings.SsoExpiredMessage);
            }
            if (_db.Users.FirstOrDefault(item => item.Email.ToLower().Equals(email)) != null)
            {
                throw new Exception(ResourceStrings.UserExistMessage);
            }
            _db.Users.Add(user);
            _db.SaveChanges();
            string result = JsonUtility.ObjectToJson(user);
            WriteResponce(result);
        }

        [WebMethod]
        [ScriptMethod(UseHttpGet = true)]
        public void Login(string email, string password)
        {
            int hash = password.GetHashCode();
            email = email.ToLower();
            string result = string.Empty;
            User user = _db.Users.FirstOrDefault(item => item.Email.ToLower().Equals(email));
            if (user != null && user.PasswordHash.Equals(hash))
            {
                if (user.IsEnabled == true)
                {
                    SSO sso = _db.SSOes.FirstOrDefault(item => item.UserId.Equals(user.UserId)) ?? new SSO();
                    sso.SSOValue = SSOUtility.GenerateSSO();
                    sso.ExpirationDate = DateTime.Now.AddMinutes(Settings.SessionExpirationTimeout);
                    sso.User = user;
                    _db.SSOes.Add(sso);
                    _db.SaveChanges();
                    result = sso.SSOValue.ToString();
                }
                else
                {
                    result = ResourceStrings.UserDeactivatedMessage;
                }
            }
            else
            {
                result = ResourceStrings.UserNotExistMessage;
            }
            WriteResponce(result);
        }

        [WebMethod]
        [Roles(new[] { Roles.Admin })]
        [ScriptMethod(UseHttpGet = true)]
        public void UpdateUser(string userJson, int ssoId)
        {
            User user = JsonUtility.JsonToObject<User>(userJson);
            if (!ValidateUser(ssoId))
            {
                throw new Exception(ResourceStrings.SsoExpiredMessage);
            }
            User currentUser = _db.Users.FirstOrDefault(item => item.UserId.Equals(user.UserId));
            if (currentUser != null)
            {
                currentUser.FirstName = string.IsNullOrEmpty(user.FirstName)
                    ? currentUser.FirstName
                    : user.FirstName;
                currentUser.LastName = string.IsNullOrEmpty(user.LastName)
                    ? currentUser.LastName
                    : user.LastName;
                currentUser.IsEnabled = user.IsEnabled;
                _db.SaveChanges();
            }
        }

        [WebMethod]
        [ScriptMethod(UseHttpGet = true)]
        public void GetUser(int userId, int ssoId)
        {
            if (!ValidateUser(ssoId))
            {
                throw new Exception(ResourceStrings.SsoExpiredMessage);
            }
            User currentUser = _db.Users.FirstOrDefault(item => item.UserId.Equals(userId));
            string result = currentUser != null ? JsonUtility.ObjectToJson(currentUser) : ResourceStrings.UserNotExistMessage;
            WriteResponce(result);
        }

        [WebMethod]
        public void Logout(int ssoId)
        {
            User user = GetUserBySSO(ssoId);
            if (user != null)
            {
                SSO sso = _db.SSOes.FirstOrDefault(item => item.UserId.Equals(user.UserId)) ?? new SSO();
                sso.ExpirationDate = DateTime.Now.AddMinutes(-Settings.SessionExpirationTimeout + 1);
                _db.SaveChanges();
            }
        }

        [WebMethod]
        [Roles(new[] { Roles.Admin, Roles.ChiefTrainer })]
        [ScriptMethod(UseHttpGet = true)]
        public void CreateWorkout(string workoutJson, int ssoId)
        {
            Workout workout = JsonUtility.JsonToObject<Workout>(workoutJson);
            if (!ValidateUser(ssoId))
            {
                throw new Exception(ResourceStrings.SsoExpiredMessage);
            }
            User user = GetUserBySSO(ssoId);
            workout.User = user;
            foreach (Exercis exercise in workout.Exercises)
            {
                exercise.ExerciseType = GetExerciseType(exercise.ExerciseTypeId);
            }
            workout = _db.Workouts.Add(workout);
            _db.SaveChanges();
            string result = workout.WorkoutId.ToString();
            WriteResponce(result);
        }

        private ExerciseType GetExerciseType(int id)
        {
            return _db.ExerciseTypes.FirstOrDefault(type => type.ExerciseTypeId.Equals(id));
        }

        [WebMethod]
        [ScriptMethod(UseHttpGet = true)]
        public void UpdateWorkout(string workoutJson, int ssoId)
        {
            Workout workout = JsonUtility.JsonToObject<Workout>(workoutJson);
            string result = string.Empty;
            if (!ValidateUser(ssoId))
            {
                throw new Exception(ResourceStrings.SsoExpiredMessage);
            }
            Workout storedWorkout = _db.Workouts.FirstOrDefault(item => item.WorkoutId.Equals(workout.WorkoutId));
            if (storedWorkout != null)
            {
                _db.Exercises.RemoveRange(storedWorkout.Exercises);
                MergeWorkouts(workout, storedWorkout);
                _db.SaveChanges();
            }
            else
            {
                result = ResourceStrings.WorkoutNotFoundMessage;
            }
            WriteResponce(result);
        }

        [WebMethod]
        [Roles(new[] { Roles.Admin, Roles.ChiefTrainer })]
        [ScriptMethod(UseHttpGet = true)]
        public void DeleteWorkout(int workoutId, int ssoId)
        {
            string result = string.Empty;
            if (!ValidateUser(ssoId))
            {
                throw new Exception(ResourceStrings.SsoExpiredMessage);
            }
            Workout workout = _db.Workouts.FirstOrDefault(item => item.WorkoutId.Equals(workoutId));
            if (workout != null)
            {
                var exercises = _db.Exercises.Where(item => item.WorkoutId.Equals(workoutId));
                foreach (Exercis exercise in exercises)
                {
                    _db.Exercises.Remove(exercise);
                }
                _db.Workouts.Remove(workout);
                _db.SaveChanges();
            }
            else
            {
                result = ResourceStrings.WorkoutNotFoundMessage;
            }
            WriteResponce(result);
        }

        [WebMethod]
        [ScriptMethod(UseHttpGet = true)]
        public void GetWorkout(int workoutId, int ssoId)
        {
            if (!ValidateUser(ssoId))
            {
                throw new Exception(ResourceStrings.SsoExpiredMessage);
            }
            Workout workout = _db.Workouts.FirstOrDefault(item => item.WorkoutId.Equals(workoutId));
            string result = workout != null ? JsonUtility.ObjectToJson(workout) : ResourceStrings.WorkoutNotFoundMessage;
            WriteResponce(result);
        }

        [WebMethod]
        [ScriptMethod(UseHttpGet = true)]
        public void GetUserWorkouts(int userId, int ssoId)
        {
            if (!ValidateUser(ssoId))
            {
                throw new Exception(ResourceStrings.SsoExpiredMessage);
            }
            var workouts = _db.Workouts.Where(item => item.UserId.Equals(userId)).ToList();
            string result = JsonUtility.ObjectToJson(workouts);
            WriteResponce(result);
        }

        [WebMethod]
        [ScriptMethod(UseHttpGet = true)]
        public void GetClientWorkouts(int clientId, int ssoId)
        {
            if (!ValidateUser(ssoId))
            {
                throw new Exception(ResourceStrings.SsoExpiredMessage);
            }
            var workouts = _db.Workouts.Where(item => item.ClientId.Equals(clientId));
            string result = JsonUtility.ObjectToJson(workouts);
            WriteResponce(result);
        }

        [WebMethod]
        [Roles(new[] { Roles.Admin, Roles.ChiefTrainer })]
        [ScriptMethod(UseHttpGet = true)]
        public void CreateExerciseType(string exerciseTypeJson, int ssoId)
        {
            ExerciseType type = JsonUtility.JsonToObject<ExerciseType>(exerciseTypeJson);
            if (!ValidateUser(ssoId))
            {
                throw new Exception(ResourceStrings.SsoExpiredMessage);
            }
            type = _db.ExerciseTypes.Add(type);
            _db.SaveChanges();
            string result = JsonUtility.ObjectToJson(type);
            WriteResponce(result);
        }

        [WebMethod]
        [Roles(new[] { Roles.Admin, Roles.ChiefTrainer })]
        [ScriptMethod(UseHttpGet = true)]
        public void DeleteExerciseType(int exerciseTypeId, int ssoId)
        {
            string result = string.Empty;
            if (!ValidateUser(ssoId))
            {
                throw new Exception(ResourceStrings.SsoExpiredMessage);
            }
            ExerciseType type = _db.ExerciseTypes.FirstOrDefault(item => item.ExerciseTypeId.Equals(exerciseTypeId));
            if (type != null)
            {
                _db.ExerciseTypes.Remove(type);
                _db.SaveChanges();
            }
            else
            {
                result = ResourceStrings.ExerciseTypeNotFoundMessage;
            }
            WriteResponce(result);
        }

        [WebMethod]
        [Roles(new[] { Roles.Admin, Roles.ChiefTrainer })]
        [ScriptMethod(UseHttpGet = true)]
        public void UpdateExerciseType(string exerciseTypeJson, int ssoId)
        {
            ExerciseType sourceType = JsonUtility.JsonToObject<ExerciseType>(exerciseTypeJson);
            string result = string.Empty;
            if (!ValidateUser(ssoId))
            {
                throw new Exception(ResourceStrings.SsoExpiredMessage);
            }
            ExerciseType type = _db.ExerciseTypes.FirstOrDefault(item => item.ExerciseTypeId.Equals(sourceType.ExerciseTypeId));
            if (type != null)
            {
                MergeExerciseTypes(sourceType, type);
                _db.SaveChanges();
            }
            else
            {
                result = ResourceStrings.ExerciseTypeNotFoundMessage;
            }
            WriteResponce(result);
        }

        [WebMethod]
        [ScriptMethod(UseHttpGet = true)]
        public void GetExerciseTypes(int ssoId)
        {
            if (!ValidateUser(ssoId))
            {
                throw new Exception(ResourceStrings.SsoExpiredMessage);
            }
            string result = JsonUtility.ObjectToJson(_db.ExerciseTypes);
            WriteResponce(result);
        }

        [WebMethod]
        [ScriptMethod(UseHttpGet = true)]
        public void WhoAmI(int ssoId)
        {
            if (!ValidateUser(ssoId))
            {
                throw new Exception(ResourceStrings.SsoExpiredMessage);
            }
            User user = GetUserBySSO(ssoId);
            string result = JsonUtility.ObjectToJson(user);
            WriteResponce(result);
        }

        private ExerciseType MergeExerciseTypes(ExerciseType source, ExerciseType target)
        {
            if (source.Duration != null)
            {
                target.Duration = source.Duration;
            }
            if (source.Description != null)
            {
                target.Description = source.Description;
            }
            if (source.Title != null)
            {
                target.Title = source.Title;
            }
            return target;
        }

        private Workout MergeWorkouts(Workout source, Workout target)
        {
            if (source.UserId > 0)
            {
                target.UserId = source.UserId;
            }
            if (source.Date.Ticks > 0)
            {
                target.Date = source.Date;
            }
            if (source.ClientId > 0)
            {
                target.ClientId = source.ClientId;
            }
            target.IsCompleted = source.IsCompleted;
            if (source.Exercises != null)
            {
                target.Exercises = source.Exercises;
            }
            return target;
        }

        private bool ValidateUser(int ssoId)
        {
            ValidateRoles(ssoId);
            SSO sso = _db?.SSOes.FirstOrDefault(item => item.SSOValue.Equals(ssoId));
            return sso != null && DateTime.Now.AddMinutes(-1 * Settings.SessionExpirationTimeout) < sso.ExpirationDate;
        }

        private User GetUserBySSO(int ssoId)
        {
            var sso = _db?.SSOes.FirstOrDefault(item => item.SSOValue.Equals(ssoId));
            return sso == null ? null : _db?.Users.FirstOrDefault(user => user.UserId.Equals(sso.UserId));
        }

        private void WriteResponce(string json)
        {
            Context.Response.Clear();
            Context.Response.ContentType = "application/json";
            Context.Response.AddHeader("content-disposition", "attachment; filename=export.json");
            Context.Response.AddHeader("content-length", (Encoding.UTF8.GetByteCount(json)).ToString());
            Context.Response.Flush();
            Context.Response.Write(json);
        }

        private void ValidateRoles(int ssoId)
        {
            StackTrace stackTrace = new StackTrace();
            var roleAttribute = stackTrace.GetFrame(2).GetMethod().GetCustomAttributes(typeof(RolesAttribute));
            if (roleAttribute.Any())
            {
                var roles = ((RolesAttribute) roleAttribute.FirstOrDefault())?.Roles;
                if (roles != null)
                {
                    var user = GetUserBySSO(ssoId);
                    if (user == null)
                    {
                        throw new Exception(ResourceStrings.UserNotLoggedMessage);
                    }
                    if (roles.Contains(Roles.Admin) && user.IsAdmin != true && roles.Length == 1)
                    {
                        throw new Exception(string.Format(ResourceStrings.RoleErrorMessageFormat, Roles.Admin));
                    }
                    if (roles.Contains(Roles.ChiefTrainer) && (user.IsChiefTrainer != true && user.IsAdmin != true))
                    {
                        throw new Exception(string.Format(ResourceStrings.RoleErrorMessageFormat, Roles.ChiefTrainer));
                    }
                }
            }
        }

    }
}