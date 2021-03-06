﻿using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Runtime.InteropServices;
using MicroSocialServer.Schema;

namespace MicroSocialServer
{
    internal class DatabaseManager
    {
        private const string DatabaseFilename = "database.db";
        private SQLiteConnection _databaseConnection;

        public void Connect()
        {
            _databaseConnection = new SQLiteConnection("Data Source=" + DatabaseFilename + ";Version=3;");
            _databaseConnection.Open();
        }

        public void Close()
        {
            if (_databaseConnection != null)
            {
                _databaseConnection.Close();
            }
        }

        // USER MANAGEMENT

        public void AddUser(User user)
        {
            var sqlCommand = String.Format(
                "INSERT INTO Users (username, password_hash, full_name, email) VALUES ('{0}', '{1}', '{2}', '{3}');",
                user.username, user.passwordHash, user.fullName, user.email
                );

            var command = new SQLiteCommand(sqlCommand, _databaseConnection);
            command.ExecuteNonQuery();
        }

        public List<User> GetUsers()
        {
            var users = new List<User>();

            var sqlQuery =
                "SELECT * FROM Users ORDER BY username ASC";

            var command = new SQLiteCommand(sqlQuery, _databaseConnection);
            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var user = new User();
                user.username = (string) reader["username"];
                user.fullName = (string) reader["full_name"];
                user.email = (string)reader["email"];

                users.Add(user);
            }

            return users;
        }

        public bool CheckPassword(string username, string plaintextPassword)
        {
            var sqlQuery = String.Format(
                "SELECT * FROM Users WHERE username=\"{0}\"",
                username);

            var command = new SQLiteCommand(sqlQuery, _databaseConnection);
            var reader = command.ExecuteReader();

            if (reader.HasRows)
            {
                reader.Read();
                var hashedPassword = (string) reader["password_hash"];

                reader.Close();

                return BCrypt.Net.BCrypt.Verify(plaintextPassword, hashedPassword);
            }
            else
            {
                return false;
            }
        }

        // SESSION MANAGEMENT

        public bool CheckSession(int sessionId)
        {
            var sqlQuery = String.Format(
                "SELECT * FROM Sessions WHERE session_id={0}",
                sessionId);

            var command = new SQLiteCommand(sqlQuery, _databaseConnection);
            var reader = command.ExecuteReader();

            var hasRows = reader.HasRows;
            reader.Close();

            return hasRows;
        }

        public int AddSession(string username)
        {
            var findOldDataQuery = String.Format(
                "SELECT * FROM Sessions WHERE username=\"{0}\"",
                username
                );

            var findCommand = new SQLiteCommand(findOldDataQuery, _databaseConnection);
            var findReader = findCommand.ExecuteReader();

            if (findReader.HasRows)
            {
                var removeOldDataQuery = String.Format(
                    "DELETE FROM Sessions WHERE username=\"{0}\"",
                    username
                    );

                var deleteCommand = new SQLiteCommand(removeOldDataQuery, _databaseConnection);
                deleteCommand.ExecuteNonQuery();
            }

            findReader.Close();

            var sqlCommand = String.Format(
                "INSERT INTO Sessions (username) VALUES ('{0}');",
                username
                );

            var command = new SQLiteCommand(sqlCommand, _databaseConnection);
            var rowsAffected = command.ExecuteNonQuery();

            if (rowsAffected > 0)
            {
                var sqlQuery = String.Format(
                    "SELECT * FROM Sessions WHERE username=\"{0}\"",
                    username);

                var query = new SQLiteCommand(sqlQuery, _databaseConnection);
                var reader = query.ExecuteReader();

                reader.Read();
                var sessionId = reader.GetInt32(1);
                reader.Close();
                //return (int)reader["session_id"];
                return sessionId;
            }
            return 0;
        }

        public void DeleteSession(string username)
        {
            var removeOldDataQuery = String.Format(
                "DELETE FROM Sessions WHERE username=\"{0}\"",
                username
                );

            var deleteCommand = new SQLiteCommand(removeOldDataQuery, _databaseConnection);
            deleteCommand.ExecuteNonQuery();
        }

        public User GetUserFromSession(int session)
        {
            var sql = String.Format(
                "SELECT * FROM Sessions WHERE session_id={0}",
                session);

            var command = new SQLiteCommand(sql, _databaseConnection);
            var reader = command.ExecuteReader();

            if (reader.HasRows)
            {
                reader.Read();
                string username = (string)reader["username"];

                var userSql = String.Format(
                    "SELECT * FROM Users WHERE username='{0}'",
                    username);

                var userCommand = new SQLiteCommand(userSql, _databaseConnection);
                var userReader = userCommand.ExecuteReader();
                userReader.Read();

                var user = new User();
                user.username = (string) userReader["username"];
                user.fullName = (string) userReader["full_name"];
                user.email = (string) userReader["email"];

                reader.Close();
                userReader.Close();
                return user;
            }
            else
            {
                reader.Close();
                return null;
            }
        }

        public User GetUserFromUsername(string username)
        {
            var sql = String.Format(
                "SELECT * FROM Users WHERE username='{0}'",
                username);

            var command = new SQLiteCommand(sql, _databaseConnection);
            var reader = command.ExecuteReader();

            if (reader.HasRows)
            {
                reader.Read();
                var user = new User();
                user.username = (string) reader["username"];
                user.email = (string) reader["email"];
                user.fullName = (string)reader["full_name"];

                reader.Close();
                return user;
            }
            else
            {
                reader.Close();
                return null;
            }
        }

        // STATUS MANAGEMENT

        public void AddStatus(Status status)
        {
            var sql = String.Format(
                "INSERT INTO Statuses (username, time, status_body) VALUES ('{0}', DATETIME('NOW'), '{1}')",
                status.poster.username, status.statusContent
            );

            var command = new SQLiteCommand(sql, _databaseConnection);
            command.ExecuteNonQuery();
        }

        public List<Status> GetStatuses(int first, int last)
        {
            var statuses = new List<Status>();

            var sql = String.Format(
                "SELECT * FROM Statuses ORDER BY ID DESC LIMIT {0}",
                first + last + 1
                );

            var command = new SQLiteCommand(sql, _databaseConnection);
            var reader = command.ExecuteReader();

            int i = 0;
            while (reader.Read())
            {
                var newStatus = new Status();
                newStatus.poster = GetUserFromUsername((string) reader["username"]);
                newStatus.statusContent = (string) reader["status_body"];
                newStatus.time = ((DateTime) reader["time"]);

                if (i >= first)
                    statuses.Add(newStatus);

                i++;
            }

            reader.Close();
            return statuses;
        }

        // MESSAGE MANAGEMENT

        public void AddMessage(Message message)
        {
            var sql = String.Format(
                "INSERT INTO Messages (fromUser, toUser, time, message) VALUES ('{0}', '{1}', DATETIME('NOW'), '{2}')",
                message.senderName, message.recipientName, message.messageBody
                );

            var command = new SQLiteCommand(sql, _databaseConnection);
            command.ExecuteNonQuery();
        }

        public List<Message> GetMessages(string user1, string user2, int first, int last)
        {
            var messages = new List<Message>();

            var sql = String.Format(
                //"SELECT * FROM Messages WHERE (from='{0}' AND to='{1}') OR (from='{1}' AND to='{0}') ORDER BY ID DESC LIMIT {2}",
                  //WHERE (from = '{0}' AND to = '{1}')
                  //OR (from = '{1}' AND to = '{0}') 
                @"SELECT *
                  FROM Messages 
                  WHERE (fromUser = '{0}' AND toUser = '{1}')
                  OR (fromUser = '{1}' AND toUser = '{0}') 
                  ORDER BY ID DESC 
                  LIMIT {2}",
                user1, user2, first + last + 1
                );

            var command = new SQLiteCommand(sql, _databaseConnection);
            var reader = command.ExecuteReader();

            int i = 0;
            while (reader.Read())
            {
                var message = new Message();
                message.senderName = (string) reader["fromUser"];
                message.recipientName = (string) reader["toUser"];
                message.time = (DateTime) reader["time"];
                message.messageBody = (string) reader["message"];

                var emailSql = String.Format(
                    @"SELECT email
                      FROM Users 
                      WHERE username = '{0}'",
                    (string) reader["fromUser"]);

                var emailCommand = new SQLiteCommand(emailSql, _databaseConnection);
                var emailReader = emailCommand.ExecuteReader();
                emailReader.Read();
                message.senderEmail = (string) emailReader["email"];
                emailReader.Close();

                if (i >= first)
                    messages.Add(message);

                i++;
            }

            reader.Close();
            return messages;
        }

        public List<Message> GetLatestMessages(string user, int first, int last)
        {
            var messages = new List<Message>();

            var sql = String.Format(
                    @"SELECT username
                      FROM Users
                      WHERE 1                      
                    ");

            var command = new SQLiteCommand(sql, _databaseConnection);
            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var username = (string) reader["username"];

                var userSql = String.Format(
                            @"SELECT *
                            FROM Messages
                            WHERE (fromUser = '{0}' AND toUser = '{1}')
                            OR (fromUser = '{1}' AND toUser = '{0}')
                            ORDER BY ID DESC
                            LIMIT 1
                            ", user, username);

                var userCommand = new SQLiteCommand(userSql, _databaseConnection);
                var userReader = userCommand.ExecuteReader();

                while (userReader.Read())
                {
                    var message = new Message();
                    message.senderName = (string)userReader["fromUser"];
                    message.recipientName = (string)userReader["toUser"];
                    message.time = (DateTime)userReader["time"];
                    message.messageBody = (string)userReader["message"];

                    var emailSql = String.Format(
                    @"SELECT email
                    FROM Users 
                    WHERE username = '{0}'",
                    message.senderName);

                    var fullNameSql = String.Format(
                    @"SELECT full_name
                    FROM Users
                    WHERE username = '{0}'",
                    message.senderName == user ? message.recipientName : message.senderName);

                    var emailCommand = new SQLiteCommand(emailSql, _databaseConnection);
                    var emailReader = emailCommand.ExecuteReader();
                    emailReader.Read();
                    message.senderEmail = (string)emailReader["email"];
                    emailReader.Close();

                    var fullNameCommand = new SQLiteCommand(fullNameSql, _databaseConnection);
                    var fullNameReader = fullNameCommand.ExecuteReader();
                    fullNameReader.Read();
                    message.displayName = (string)fullNameReader["full_name"];
                    fullNameReader.Close();

                    messages.Add(message);
                }

                userReader.Close();
            }

            reader.Close();

            //if (first + 1 > messages.Count)
            //{
            //    return new List<Message>(); // return an empty list since we're out of bounds
            //}
            //else if (last + 1 > messages.Count)
            //{
            //    return messages.GetRange(first, messages.Count - first);
            //}
            //else
            //{
            //    return messages.GetRange(first, last - first);
            //}

            return messages; // disabled the range serverside
        }

    }
}