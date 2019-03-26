using AdoAuctions.Extensions;
using AdoAuctions.ExternalModels;
using AdoAuctions.Models;
using AdoAuctions.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace AdoAuctions.Services
{
    public class AccountService
    {
        public void OpenOrganization(OpenOrganizationViewModel viewModel)
        {
            DataSet applicationDataSet = new DataSet();
            DataSet identityDataSet = new DataSet();

            string organizationsTable = "[dbo].[Organizations]";
            string employeeTable = "[dbo].[Employees]";

            string userTable = "[dbo].[ApplicationUser]";
            string userSignInTable = "[dbo].[ApplicationUserSignInHistories]";
            string userPasswordTable = "[dbo].[ApplicationUserPasswordHistories]";

            using (TransactionScope transactionScope = new TransactionScope())
            {
                try
                {
                    using (SqlConnection applicationConnection = new SqlConnection(ApplicationSettings.APPLICATION_CONNECTION_STRING))
                    {
                        applicationConnection.Open();

                        string selectOrganizationByIdentificatorsSql = $"select * from {organizationsTable} " +
                            $"where [IdentificationNumber] = {viewModel.OrganizationIdentificationNumber}";

                        string selectOrganizationsSql = $"select * from {organizationsTable}";

                        using (SqlDataAdapter applicationAdapter = new SqlDataAdapter(selectOrganizationByIdentificatorsSql, applicationConnection))
                        {
                            applicationAdapter.Fill(applicationDataSet, organizationsTable);
                            SqlCommandBuilder applicationCommandBuilder = new SqlCommandBuilder(applicationAdapter);
                            bool isOrganizationAlreadyExist = applicationDataSet
                                .Tables[organizationsTable].Rows.Count != 0;

                            if (isOrganizationAlreadyExist)
                                throw new ApplicationException($"Already has an organization with IdentificationNumber = {viewModel.OrganizationIdentificationNumber}");

                            applicationDataSet.Clear();

                            Organization organization = new Organization()
                            {
                                FullName = viewModel.OrganizationFullName,
                                IdentificationNumber = viewModel.OrganizationIdentificationNumber,
                                OrganizationTypeId = viewModel.OrganizationTypeId,
                                RegistrationDate = DateTime.Now.ToString("yyyy-MM-dd")
                            };

                            applicationAdapter.SelectCommand = new SqlCommand(selectOrganizationsSql, applicationConnection);
                            applicationCommandBuilder = new SqlCommandBuilder(applicationAdapter);

                            applicationAdapter.Fill(applicationDataSet, organizationsTable);
                            var dataRow = applicationDataSet.Tables[organizationsTable].NewRowWithData(organization);
                            applicationDataSet.Tables[organizationsTable].Rows.Add(dataRow);
                            applicationAdapter.Update(applicationDataSet, organizationsTable);

                            Employee employee = new Employee()
                            {
                                FirstName = viewModel.CeoFirstName,
                                LastName = viewModel.CeoLastName,
                                MiddleName = viewModel.CeoMiddleName,
                                Email = viewModel.Email,
                                DoB = viewModel.DoB,
                                OrganizationId = Guid.NewGuid().ToString()
                            };
                            string selectEmployeeSql = $"select * from {employeeTable}";

                            applicationAdapter.SelectCommand = new SqlCommand(selectEmployeeSql, applicationConnection);
                            applicationCommandBuilder = new SqlCommandBuilder(applicationAdapter);
                            applicationAdapter.Fill(applicationDataSet, employeeTable);

                            dataRow = applicationDataSet.Tables[employeeTable].NewRowWithData(employee);
                            applicationDataSet.Tables[employeeTable].Rows.Add(dataRow);
                            applicationAdapter.Update(applicationDataSet, employeeTable);

                            //transactionScope.Complete();
                        }
                    }
                    using (SqlConnection identityConnection = new SqlConnection(ApplicationSettings.IDENTITY_CONNECTION_STRING))
                    {
                        identityConnection.Open();
                        
                        string selectUserSql = $"select * from {userTable}";

                        using (SqlDataAdapter identityAdapter = new SqlDataAdapter(selectUserSql, identityConnection))
                        {
                            identityAdapter.Fill(identityDataSet, userTable);
                            SqlCommandBuilder identityCommandBuilder = new SqlCommandBuilder(identityAdapter);
                            identityDataSet.Clear(); //??

                            ApplicationUser user = new ApplicationUser()
                            {
                                Id = Guid.NewGuid().ToString(),
                                Email = viewModel.Email,
                                IsActivatedAccount = true,
                                FailedSigninCount = 0,
                                IsBlockedBySystem = false,
                                CreationDate = DateTime.Now
                            };                            

                            identityAdapter.SelectCommand = new SqlCommand(selectUserSql, identityConnection);
                            identityCommandBuilder = new SqlCommandBuilder(identityAdapter);

                            identityAdapter.Fill(identityDataSet, userTable);
                            var dataRow = identityDataSet.Tables[userTable].NewRowWithData(user);
                            identityDataSet.Tables[userTable].Rows.Add(dataRow);
                            identityAdapter.Update(identityDataSet, userTable);


                            ApplicationUserPasswordHistories userPassword = new ApplicationUserPasswordHistories()
                            {
                                ApplicationUserId = user.Id,
                                SetupDate = DateTime.Now,
                                PasswordHash = viewModel.Password
                            };

                            string userPawwordSql= $"select * from {userPasswordTable}";
                            identityAdapter.SelectCommand = new SqlCommand(userPawwordSql, identityConnection);
                            identityCommandBuilder = new SqlCommandBuilder(identityAdapter);

                            identityAdapter.Fill(identityDataSet, userPasswordTable);
                            dataRow = identityDataSet.Tables[userPasswordTable].NewRowWithData(userPassword);
                            identityDataSet.Tables[userPasswordTable].Rows.Add(dataRow);
                            identityAdapter.Update(identityDataSet, userPasswordTable);


                            GeoLocationInfo geoLocationInfo = GetGeolocationInfo();
                            ApplicationUserSignInHistories userSignIn = new ApplicationUserSignInHistories()
                            {
                                ApplicationUserId = user.Id,
                                SignInTime = DateTime.Now,
                                MachineIp = geoLocationInfo.ip,
                                IpToGeoCountryCode = geoLocationInfo.country_name,
                                IpToGeoCityName = geoLocationInfo.city,
                                IpToGeoLatitude = geoLocationInfo.latitude,
                                IpToGeoLongitude = geoLocationInfo.longitude
                            };

                            string userSignInSql= $"select * from {userSignInTable}";
                            identityAdapter.SelectCommand = new SqlCommand(userSignInSql, identityConnection);
                            identityCommandBuilder = new SqlCommandBuilder(identityAdapter);

                            identityAdapter.Fill(identityDataSet, userSignInTable);
                            dataRow = identityDataSet.Tables[userSignInTable].NewRowWithData(userSignIn);
                            identityDataSet.Tables[userSignInTable].Rows.Add(dataRow);
                            identityAdapter.Update(identityDataSet, userSignInTable);

                        }
                    }
                    transactionScope.Complete();
                }
                catch (Exception)
                {
                    throw new ApplicationException("Have no connection");
                }
            }
        }

        public void ChangeUserPassword(ChangeUserPasswordViewModel viewModel)
        {
            //if (viewModel.newPassword != viewModel.newPasswordConfirmation)
            //    throw new ApplicationException("Passwords do not match");

            DataSet identityDataSet = new DataSet();
            string userTable = "[dbo].[ApplicationUser]";
            string userSignInTable = "[dbo].[ApplicationUserSignInHistories]";
            string userPasswordTable = "[dbo].[ApplicationUserPasswordHistories]";

            using (SqlConnection identityConnection=new SqlConnection(ApplicationSettings.IDENTITY_CONNECTION_STRING))
            {
                identityConnection.Open();
                string selectUserByIdSql = $"select * from {userTable} where [Email]={viewModel.Email}";

                using (SqlDataAdapter identityAdapter=new SqlDataAdapter(selectUserByIdSql, identityConnection))
                {
                    identityAdapter.Fill(identityDataSet, userTable);
                    SqlCommandBuilder identityCommandBuilder = new SqlCommandBuilder(identityAdapter);
                    bool isUserExists = identityDataSet.Tables[userTable].Rows.Count != 0;
                    if (!isUserExists)
                        throw new ApplicationException($"User with email {viewModel.Email} does not exist");

                    string Id = identityDataSet.Tables[userTable].Rows[0].Field<string>("Id");
                    identityDataSet.Clear();

                    string selectUserPasswordSql = $"select top 1 from {userPasswordTable} where [ApplicationUserId]={Id}";



                }
            }
        }

        public GeoLocationInfo GetGeolocationInfo()
        {
            WebClient webClient = new WebClient();
            string externalIp = webClient
                .DownloadString("http://icanhazip.com");

            string ipStackAccessKey = "cb6a8892805bdd4727b7669b1f584318";
            string ipStackUrl = $"api.ipstack.com/{externalIp}?access_key={ipStackAccessKey}";
            ipStackUrl = "http://" + ipStackUrl;

            string ipInfoAsJson = webClient.DownloadString(ipStackUrl);            

            GeoLocationInfo geoLocationInfo = JsonConvert.DeserializeObject<GeoLocationInfo>(ipInfoAsJson);
            return geoLocationInfo;            
        }
    }
}
