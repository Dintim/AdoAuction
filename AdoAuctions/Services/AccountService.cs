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

            string userTable = "[dbo].[ApplicationUsers]";
            string userSignInTable = "[dbo].[ApplicationUserSignInHistories]";
            string userPasswordTable = "[dbo].[ApplicationUserPasswordHistories]";
            string associatedEmpId = string.Empty;

            string applicationConnectionString = @"Integrated Security=SSPI;Persist Security Info=False;Initial Catalog=Auction.ApplicationDb;Data Source=DM-ПК\SQLEXPRESS";
            string identityConnectionString = @"Integrated Security=SSPI;Persist Security Info=False;Initial Catalog=Auction.IdentityDb;Data Source=DM-ПК\SQLEXPRESS";
            GeoLocationInfo geoLocationInfo = GetGeolocationInfo();

            using (TransactionScope transactionScope = new TransactionScope())
            {
                try
                {
                    using (SqlConnection applicationConnection = new SqlConnection(applicationConnectionString))
                    {
                        applicationConnection.Open();
                        string selectOrganizationByIdentificatorsSql = $"select * from {organizationsTable} " +
                            $"where [IdentificationNumber] = {viewModel.OrganizationIdentificationNumber}";

                        //формируем адаптер для выгрузки данных в датасет
                        using (SqlDataAdapter applicationAdapter = new SqlDataAdapter(selectOrganizationByIdentificatorsSql, applicationConnection))
                        {
                            //заполняем в датасет таблицу organizationsTable и формируем для него SqlCommandBuilder для последующего добавления данных
                            applicationAdapter.Fill(applicationDataSet, organizationsTable);
                            SqlCommandBuilder applicationCommandBuilder = new SqlCommandBuilder(applicationAdapter);

                            //проводим проверку, есть ли уже такая организация в БД
                            bool isOrganizationAlreadyExist = applicationDataSet
                                .Tables[organizationsTable].Rows.Count != 0;
                            if (isOrganizationAlreadyExist)
                                throw new ApplicationException($"Already has an organization with IdentificationNumber = {viewModel.OrganizationIdentificationNumber}");

                            //очищаем датасет для дальнейших манипуляций с БД
                            applicationDataSet.Clear();
                            Organization organization = new Organization()
                            {
                                Id = Guid.NewGuid().ToString(),
                                FullName = viewModel.OrganizationFullName,
                                IdentificationNumber = viewModel.OrganizationIdentificationNumber,
                                OrganizationTypeId = viewModel.OrganizationTypeId,
                                RegistrationDate = DateTime.Now.ToString("yyyy-MM-dd")
                            };

                            //делаем новую команду для SqlCommandBuilder - выбрать все из таблицы organizationsTable
                            string selectOrganizationsSql = $"select * from {organizationsTable}";
                            applicationAdapter.SelectCommand = new SqlCommand(selectOrganizationsSql, applicationConnection);
                            applicationCommandBuilder = new SqlCommandBuilder(applicationAdapter);

                            applicationAdapter.Fill(applicationDataSet, organizationsTable); //заполняем датасет новыми данными
                            var dataRow = applicationDataSet.Tables[organizationsTable].NewRowWithData(organization); //формируем новую строку для таблицы organizationsTable, запихиваем в нее данные объекта organization
                            applicationDataSet.Tables[organizationsTable].Rows.Add(dataRow); //добавляем строку в organizationsTable
                            applicationAdapter.Update(applicationDataSet, organizationsTable); //обновляем таблицу Organizations в БД

                            //добавляем новые данные в таблицу Employees в БД
                            applicationDataSet.Clear();
                            Employee employee = new Employee()
                            {
                                Id = Guid.NewGuid().ToString(),
                                FirstName = viewModel.CeoFirstName,
                                LastName = viewModel.CeoLastName,
                                MiddleName = viewModel.CeoMiddleName,
                                Email = viewModel.Email,
                                DoB = viewModel.DoB.ToString("yyyy-MM-dd"),
                                OrganizationId = Guid.NewGuid().ToString()
                            };
                            associatedEmpId = employee.Id;

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

                    using (SqlConnection identityConnection = new SqlConnection(identityConnectionString))
                    {
                        identityConnection.Open();
                        string selectUserByEmailSql = $"select * from {userTable} " +
                            $"where [Email] = '{viewModel.Email}'";

                        //формируем адаптер и вытаскиваем данные о юзерах с таким емейлом
                        using (SqlDataAdapter identityAdapter = new SqlDataAdapter(selectUserByEmailSql, identityConnection))
                        {
                            identityAdapter.Fill(identityDataSet, userTable);
                            SqlCommandBuilder identityCommandBuilder = new SqlCommandBuilder(identityAdapter);

                            //проверяем есть ли юзеры с таким мейлом
                            bool isUserAlreadyExist = identityDataSet
                                .Tables[userTable].Rows.Count != 0;
                            if (isUserAlreadyExist)
                                throw new ApplicationException($"Already has a user with Email = {viewModel.Email}");

                            //добавляем юзера в ApplicationUser в БД
                            identityDataSet.Clear();
                            ApplicationUser user = new ApplicationUser()
                            {
                                Id = Guid.NewGuid().ToString(),
                                Email = viewModel.Email,
                                IsActivatedAccount = true,
                                FailedSigninCount = 0,
                                IsBlockedBySystem = false,
                                AssociatedEmployeeId = associatedEmpId,
                                CreationDate = DateTime.Now.ToString("yyyy-MM-dd")
                            };

                            string selectUserSql = $"select * from {userTable}";
                            identityAdapter.SelectCommand = new SqlCommand(selectUserSql, identityConnection);
                            identityCommandBuilder = new SqlCommandBuilder(identityAdapter);

                            identityAdapter.Fill(identityDataSet, userTable);
                            var dataRow = identityDataSet.Tables[userTable].NewRowWithData(user);
                            identityDataSet.Tables[userTable].Rows.Add(dataRow);
                            identityAdapter.Update(identityDataSet, userTable);

                            //добавляем строку в ApplicationUserPasswordHistories в БД
                            identityDataSet.Clear();
                            ApplicationUserPasswordHistories userPassword = new ApplicationUserPasswordHistories()
                            {
                                Id = Guid.NewGuid().ToString(),
                                ApplicationUserId = user.Id,
                                SetupDate = DateTime.Now.ToString("yyyy-MM-dd"),
                                PasswordHash = viewModel.Password
                            };

                            string userPasswordSql = $"select * from {userPasswordTable}";
                            identityAdapter.SelectCommand = new SqlCommand(userPasswordSql, identityConnection);
                            identityCommandBuilder = new SqlCommandBuilder(identityAdapter);

                            identityAdapter.Fill(identityDataSet, userPasswordTable);
                            dataRow = identityDataSet.Tables[userPasswordTable].NewRowWithData(userPassword);
                            identityDataSet.Tables[userPasswordTable].Rows.Add(dataRow);
                            identityAdapter.Update(identityDataSet, userPasswordTable);

                            //добавляем строку в ApplicationUserSignInHistories в БД
                            identityDataSet.Clear();
                            //GeoLocationInfo geoLocationInfo = GetGeolocationInfo();
                            ApplicationUserSignInHistories userSignIn = new ApplicationUserSignInHistories()
                            {
                                Id = Guid.NewGuid().ToString(),
                                ApplicationUserId = user.Id,
                                SignInTime = DateTime.Now.ToString("yyyy-MM-dd"),
                                MachineIp = geoLocationInfo.ip,
                                IpToGeoCountryCode = geoLocationInfo.country_name,
                                IpToGeoCityName = geoLocationInfo.city,
                                IpToGeoLatitude = geoLocationInfo.latitude,
                                IpToGeoLongitude = geoLocationInfo.longitude
                            };

                            string userSignInSql = $"select * from {userSignInTable}";
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
                    throw new ApplicationException("No connection");
                }
            }
        }

        public void ChangeUserPassword(ChangeUserPasswordViewModel viewModel)
        {
            //if (viewModel.newPassword != viewModel.newPasswordConfirmation)
            //    throw new ApplicationException("Passwords do not match");
            string identityConnectionString = @"Integrated Security=SSPI;Persist Security Info=False;Initial Catalog=Auction.IdentityDb;Data Source=DM-ПК\SQLEXPRESS";

            DataSet identityDataSet = new DataSet();
            string userTable = "[dbo].[ApplicationUsers]";
            //string userSignInTable = "[dbo].[ApplicationUserSignInHistories]";
            string userPasswordTable = "[dbo].[ApplicationUserPasswordHistories]";
            string userId = string.Empty;

            using (SqlConnection identityConnection = new SqlConnection(identityConnectionString))
            {
                identityConnection.Open();

                string selectEmailPasswordSql = $"select u.Email, u.Id, p.SetupDate, p.PasswordHash " +
                    $"from {userTable} u, {userPasswordTable} p " +
                    $"where u.Id=p.ApplicationUserId  and u.Email='{viewModel.Email}'" +
                    $"order by p.SetupDate desc";

                using (SqlDataAdapter identityAdapter = new SqlDataAdapter(selectEmailPasswordSql, identityConnection))
                {
                    //проверим, есть ли новый пароль в предыдущих 5
                    identityAdapter.Fill(identityDataSet);
                    SqlCommandBuilder identityCommandBuilder = new SqlCommandBuilder(identityAdapter);
                    DataTable dataTable = identityDataSet.Tables[0];
                    userId = dataTable.Rows[0][1].ToString();

                    int cnt = 0;
                    if (dataTable.Rows.Count <= 5)
                        cnt = dataTable.Rows.Count;
                    else if (dataTable.Rows.Count > 5)
                        cnt = 5;

                    for (int i = 0; i < cnt; i++)
                    {
                        if (viewModel.newPassword == dataTable.Rows[i][3].ToString())
                            throw new ApplicationException($"{viewModel.newPassword} has already used. Choose another password");
                    }


                    //изменяем InvalidatedDate в таблице ApplicationUserPasswordHistories
                    identityDataSet.Clear();
                    string selectUserPassword = $"select * from {userPasswordTable} " +
                        $"where ApplicationUserId = '{userId}' " +
                        $"and InvalidatedDate is null";
                    identityAdapter.SelectCommand = new SqlCommand(selectUserPassword, identityConnection);
                    identityCommandBuilder = new SqlCommandBuilder(identityAdapter);

                    identityAdapter.Fill(identityDataSet);
                    DataTable table = identityDataSet.Tables[0];
                    table.Rows[0]["InvalidatedDate"] = DateTime.Now.ToString("yyyy-MM-dd");
                    identityAdapter.Update(identityDataSet);


                    //добавляем строку в ApplicationUserPasswordHistories в БД
                    identityDataSet.Clear();
                    ApplicationUserPasswordHistories userPassword = new ApplicationUserPasswordHistories()
                    {
                        Id = Guid.NewGuid().ToString(),
                        ApplicationUserId = userId,
                        SetupDate = DateTime.Now.ToString("yyyy-MM-dd"),
                        PasswordHash = viewModel.newPassword
                    };

                    string userPasswordSql = $"select * from {userPasswordTable}";
                    identityAdapter.SelectCommand = new SqlCommand(userPasswordSql, identityConnection);
                    identityCommandBuilder = new SqlCommandBuilder(identityAdapter);

                    identityAdapter.Fill(identityDataSet, userPasswordTable);
                    var dataRow = identityDataSet.Tables[userPasswordTable].NewRowWithData(userPassword);
                    identityDataSet.Tables[userPasswordTable].Rows.Add(dataRow);
                    identityAdapter.Update(identityDataSet, userPasswordTable);
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
