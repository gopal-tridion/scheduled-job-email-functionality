using System;
using System.Collections.Generic;
using SchedulerManager.Mechanism;
using System.Data.SqlClient;
using System.Configuration;
using System.Net.Mail;
using System.Net;
using System.IO;

namespace SchedulerConsoleApp.Jobs
{
    /// <summary>
    /// A simple repeatable Job.
    /// </summary>
    class RepeatableJob : Job
    {
        /// <summary>
        /// Counter used to count the number of times this job has been
        /// executed.
        /// </summary>
        private int counter = 0;
        private List<string> emailList;

        /// <summary>
        /// Get the Job Name, which reflects the class name.
        /// </summary>
        /// <returns>The class Name.</returns>
        public override string GetName()
        {
            return this.GetType().Name;
        }

        /// <summary>
        /// Execute the Job itself. Just print a message.
        /// </summary>
        public override void DoJob()
        {
            Console.WriteLine(String.Format("Job Started : \"{0}\" of the Job \"{1}\".", DateTime.Now, this.GetName()));
            emailList = GetEmailsFromDB();
            SendMail(emailList);
        }

        // This method is retrieve the users mail ids from database.
        private List<string> GetEmailsFromDB()
        {
            List<string> emailList = null;
            SqlCommand cmd = null;
            string connectionString = ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString;
            string queryString = @"SELECT USER_ID, EMAIL_ADDRESS FROM USERS_TABLE WHERE Status = 'UR' and Reminder <= 3"; // Status "R - Read, UR - Unread"

            using (SqlConnection connection =
                       new SqlConnection(connectionString))
            {
                SqlCommand command =
                    new SqlCommand(queryString, connection);
                connection.Open();
                cmd = new SqlCommand(queryString);
                cmd.Connection = connection;

                SqlDataReader reader = cmd.ExecuteReader();

                // Call Read before accessing data.
                while (reader.Read())
                {
                    emailList.Add(reader["EMAIL_ADDRESS"].ToString());
                }

                // Call Close when done reading.
                reader.Close();
            }
            return emailList;
        }

        // This method is for sending the email to users.
        protected void SendMail(List<string> emailList)
        {
            try
            {
                string mailBodyLink = "";
                bool isMailRead = false;
                SmtpClient smtpClient = new SmtpClient();
                //Mail notification
                MailMessage message = new MailMessage();
                message.Subject = "Email Subject ";
                message.Body = "Email Message";
                message.From = new MailAddress("yourmail@gmail.com");
                foreach (string toEmail in emailList)
                {
                    message.Body = mailBodyLink + "?" + "emailId=" + toEmail;
                    message.To.Add(toEmail);
                    smtpClient.Send(message);
                    isMailRead = GetMailReturnReceipt(toEmail);
                    InsertDBwithMailStatus(toEmail, isMailRead);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while sending an email : " + ex.Message);
            }
        }

        private bool GetMailReturnReceipt(string toEmail)
        {
            bool isMailRead = false;
            try
            {
                string mailBodyLink = "";
                // Create a request for the URL. 		
                WebRequest request = WebRequest.Create(mailBodyLink + "?" + "emailId=" + toEmail);
                // If required by the server, set the credentials.
                request.Credentials = CredentialCache.DefaultCredentials;
                // Get the response.
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                // Display the status.
                Console.WriteLine(response.StatusDescription);
                // Get the stream containing content returned by the server.
                Stream dataStream = response.GetResponseStream();
                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader(dataStream);
                if (reader.EndOfStream)
                {
                    isMailRead = true;
                }
                reader.Close();
                dataStream.Close();
                response.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while tracking the email : " + ex.Message);
            }
            return isMailRead;
        }

        // This method is update the status in DB if user reads mail.
        private void InsertDBwithMailStatus(string email, bool isMailRead)
        {
            try
            {
                string mailStatus = isMailRead ? "R" : "UR";
                SqlCommand cmd = null;
                string connectionString = ConfigurationManager.ConnectionStrings["DbConnectionString"].ConnectionString;

                string queryString = @"UPDATE USERS_TABLE SET Status = '+mailStatus+' WHERE EMAIL_ADDRESS = email"; // Status "R - Read, UR - Unread"

                using (SqlConnection connection =
                           new SqlConnection(connectionString))
                {
                    SqlCommand command =
                        new SqlCommand(queryString, connection);
                    connection.Open();
                    cmd = new SqlCommand(queryString);
                    cmd.Connection = connection;
                    cmd.ExecuteNonQuery();
                }

                if (isMailRead.Equals(false))
                {
                    int mailReminder = 1;
                    string reminderQueryString = @"UPDATE USERS_TABLE SET Reminder = '+mailReminder+' WHERE EMAIL_ADDRESS = email";

                    using (SqlConnection connection =
                           new SqlConnection(connectionString))
                    {
                        SqlCommand command =
                            new SqlCommand(reminderQueryString, connection);
                        connection.Open();
                        cmd = new SqlCommand(reminderQueryString);
                        cmd.Connection = connection;
                        cmd.ExecuteNonQuery();
                    }
                    mailReminder++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while updating the database with email status : " + ex.Message);
            }
        }

        /// <summary>
        /// Determines this job is repeatable.
        /// </summary>
        /// <returns>Returns true because this job is repeatable.</returns>
        public override bool IsRepeatable()
        {
            return true;
        }

        /// <summary>
        /// Determines that this job is to be executed again after
        /// 1 sec.
        /// </summary>
        /// <returns>1 sec, which is the interval this job is to be
        /// executed repeatadly.</returns>
        public override int GetRepetitionIntervalTime()
        {
            return 20000;
        }
    }
}
