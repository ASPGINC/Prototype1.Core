using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Prototype1.Security
{
    public class Aes256Encryption
    {
        private const int KEKDecryptionKeyID = -1;

        public static string Encrypt(string value, int? decryptionKeyID = null)
        {
            return Encrypt(Encoding.ASCII.GetBytes(value), decryptionKeyID);
        }

        public static string Encrypt(byte[] value, int? decryptionKeyID = null)
        {
            var key = GetDecryptionKey(decryptionKeyID);

            return Encrypt(value, key);
        }

        public static string Encrypt(string value, byte[] key)
        {
            return Encrypt(Encoding.ASCII.GetBytes(value), key);
        }

        public static string Encrypt(byte[] value, byte[] key)
        {
            using (var aes = new AesCryptoServiceProvider())
            {
                aes.KeySize = 256;
                aes.Key = key;
                aes.GenerateIV();
                var iv = aes.IV;

                var encryptor = aes.CreateEncryptor();

                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        csEncrypt.Write(value, 0, value.Length);
                    return Convert.ToBase64String(ConcatBytes(iv, msEncrypt.ToArray()));
                }
            }
        }

        public static string DecryptString(string encrypted, int decryptionKeyID)
        {
            return Encoding.ASCII.GetString(Decrypt(encrypted, decryptionKeyID));
        }

        public static byte[] Decrypt(string encrypted, int decryptionKeyID)
        {
            var key = GetDecryptionKey(decryptionKeyID);

            return Decrypt(encrypted, key);
        }

        public static string DecryptString(string encrypted, byte[] key)
        {
            return Encoding.ASCII.GetString(Decrypt(encrypted, key));
        }

        public static byte[] Decrypt(string encrypted, byte[] key)
        {
            using (var aes = new AesCryptoServiceProvider())
            {
                var ivAndValue = Convert.FromBase64String(encrypted);
                byte[] iv, value;

                SplitBytes(ivAndValue, 16, out iv, out value);

                aes.KeySize = 256;
                aes.Key = key;
                aes.IV = iv;

                var decryptor = aes.CreateDecryptor();

                var decrypted = new List<byte>();
                using (var msDecrypt = new MemoryStream(value))
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    int b;
                    while ((b = csDecrypt.ReadByte()) != -1)
                        decrypted.Add((byte)b);
                }

                return decrypted.ToArray();
            }
        }

        private static byte[] ConcatBytes(byte[] a, byte[] b)
        {
            var c = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, c, 0, a.Length);
            Buffer.BlockCopy(b, 0, c, a.Length, b.Length);

            return c;
        }

        private static void SplitBytes(byte[] a, int splitIndex, out byte[] front, out byte[] back)
        {
            front = new byte[splitIndex];
            back = new byte[a.Length - splitIndex];
            Buffer.BlockCopy(a, 0, front, 0, splitIndex);
            Buffer.BlockCopy(a, splitIndex, back, 0, a.Length - splitIndex);
        }

        public static readonly byte[] AditionalEntropy = { 9, 8, 7, 6, 5, 2, 5, 6, 2, 51, 6, 12, 13, 5, 3, 2, 3, 1 };

        private static byte[] KEK
        {
            get
            {
                return
                    ProtectedData.Unprotect(
                        Convert.FromBase64String(ConfigurationManager.AppSettings["KEKEncrypted"]), AditionalEntropy,
                        DataProtectionScope.LocalMachine);
            }
        }

        private static int? _newestDecryptionKeyID;
        public static int NewestDecryptionKeyID
        {
            get
            {
                if (!_newestDecryptionKeyID.HasValue)
                {
                    using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["Prototype1"].ConnectionString))
                    using (var command = connection.CreateCommand())
                    {
                        connection.Open();
                        command.CommandText = string.Format(@"SELECT MAX(DecryptionKeyID) FROM DecryptionKey");
                        using (var reader = command.ExecuteReader(CommandBehavior.CloseConnection))
                        {
                            reader.Read();
                            _newestDecryptionKeyID = int.Parse(reader[0].ToString());
                        }
                    }
                }
                return _newestDecryptionKeyID.Value;
            }
        }

        private static int GenerateDecryptionKey()
        {
            var key = Encrypt(CreateKey(), KEKDecryptionKeyID);
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["Prototype1"].ConnectionString))
            using (var command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = string.Format(@"
                    INSERT INTO DecryptionKey ([Key], DateCreated)
                    OUTPUT Inserted.DecryptionKeyID
                    VALUES ('{0}', getdate())", key);
                using (var reader = command.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    reader.Read();
                    var id = int.Parse(reader[0].ToString());
                    DecryptionKeys[id] = key;
                    _newestDecryptionKeyID = id;
                    return id;
                }
            }
        }

        private static readonly ConcurrentDictionary<int, string> DecryptionKeys = new ConcurrentDictionary<int, string>();
        private static byte[] GetDecryptionKey(int? decryptionKeyID = null)
        {
            if (decryptionKeyID == KEKDecryptionKeyID)
                return KEK;

            var id = decryptionKeyID ?? NewestDecryptionKeyID;
            if (!DecryptionKeys.ContainsKey(id))
            {
                using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["Prototype1"].ConnectionString))
                using (var command = connection.CreateCommand())
                {
                    connection.Open();
                    command.CommandText = "SELECT TOP(1) [Key] from DecryptionKey " +
                                          (decryptionKeyID.HasValue
                                              ? "WHERE DecryptionKeyID=" + decryptionKeyID.Value
                                              : "ORDER BY DecryptionKeyID DESC");
                    using (var reader = command.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        reader.Read();
                        DecryptionKeys[id] = reader[0].ToString();
                    }
                }
            }

            return Decrypt(DecryptionKeys[id], KEKDecryptionKeyID);
        }

        /// <summary>
        /// Updates all card numbers using the "old" key to be encrypted with the "new" key.
        /// </summary>
        /// <param name="oldDecryptionKeyID">If set, all cards using this key will be updated. If not set, all cards using a key older than 9 months will be updated.</param>
        /// <param name="newDecryptionKeyID">If set, all cards will be updated to this key. If not set, a new decryption key will be generated and used.</param>
        public static void RollDecryptionKey(int? oldDecryptionKeyID = null, int? newDecryptionKeyID = null)
        {
            RollDecryptionKey("CreditCard", "CreditCardID", "CCNumber", oldDecryptionKeyID, newDecryptionKeyID);
            RollDecryptionKey("OrderPayment", "OrderPaymentID", "Number", oldDecryptionKeyID, newDecryptionKeyID);
        }

        private static void RollDecryptionKey(string tableName, string idColumnName, string numberColumnName, int? oldDecryptionKeyID = null, int? newDecryptionKeyID = null)
        {
            if (newDecryptionKeyID.HasValue && oldDecryptionKeyID.HasValue && oldDecryptionKeyID.Value == newDecryptionKeyID.Value)
                throw new ArgumentException("Old Decryption Key ID cannot be the same as the New Decryption Key ID");

            newDecryptionKeyID = newDecryptionKeyID ?? GenerateDecryptionKey();

            var cards = new List<CardNumber>();

            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["Prototype1"].ConnectionString))
            using (var command = connection.CreateCommand())
            {
                connection.Open();

                if (oldDecryptionKeyID.HasValue)
                    command.CommandText = string.Format("SELECT [{1}], [{2}], DecryptionKeyID FROM [{3}] WHERE DecryptionKeyID={0}",
                                          oldDecryptionKeyID.Value, idColumnName, numberColumnName, tableName);
                else
                    command.CommandText = string.Format("SELECT c.[{0}], c.[{1}], c.DecryptionKeyID FROM [{2}] c" +
                                          " INNER JOIN DecryptionKey k on k.DecryptionKeyID = c.DecryptionKeyID" +
                                          " WHERE DateCreated < DATEADD(MONTH, -9, GETDATE())"
                                          , idColumnName, numberColumnName, tableName);

                using (var reader = command.ExecuteReader())
                    while (reader.Read())
                        cards.Add(new CardNumber
                        {
                            ID = Guid.Parse(reader[idColumnName].ToString()),
                            Number = reader[numberColumnName].ToString(),
                            DecryptionKeyID = int.Parse(reader["DecryptionKeyID"].ToString())
                        });
            }

            if (!cards.Any())
                return;

            var newKey = GetDecryptionKey(newDecryptionKeyID);

            var oldKey = (oldDecryptionKeyID.HasValue) ? GetDecryptionKey(oldDecryptionKeyID) : new byte[0];

            var keyDictionary = new Dictionary<int, byte[]>();
            cards =
                cards.Select(
                    c =>
                    {
                        if (oldKey.Length == 0) // Updating old keys and it may be different by card.
                        {
                            if (!keyDictionary.ContainsKey(c.DecryptionKeyID))
                                keyDictionary[c.DecryptionKeyID] = GetDecryptionKey(c.DecryptionKeyID);
                            oldKey = keyDictionary[c.DecryptionKeyID];
                        }

                        return new CardNumber
                        {
                            Number = Encrypt(Decrypt(c.Number, oldKey), newKey),
                            ID = c.ID,
                            DecryptionKeyID = c.DecryptionKeyID
                        };
                    }).ToList();

            Parallel.ForEach(Partition(cards, 500), new ParallelOptions { MaxDegreeOfParallelism = 3 },
                blockOfCards =>
                {
                    using (
                        var connection =
                            new SqlConnection(ConfigurationManager.ConnectionStrings["Prototype1"].ConnectionString))
                    {
                        connection.Open();
                        foreach (var card in blockOfCards)
                            using (var command = connection.CreateCommand())
                            {
                                command.CommandText =
                                    string.Format(
                                        "UPDATE [{3}] SET [{4}] = '{0}', DecryptionKeyID = {1} WHERE [{5}] = '{2}'",
                                        card.Number, newDecryptionKeyID.Value, card.ID, tableName, numberColumnName, idColumnName);
                                command.ExecuteNonQuery();
                            }
                    }
                });
        }

        public static byte[] CreateKey(int? seed = null)
        {
            var random = seed.HasValue ? new Random(seed.Value) : new Random();
            var key = new byte[32];
            for (var i = 0; i < 32; i++)
                key[i] = (byte)random.Next(byte.MinValue, byte.MaxValue);

            return key;
        }

        private struct CardNumber
        {
            public Guid ID;
            public string Number;
            public int DecryptionKeyID;
        }

        private static IEnumerable<IList<T>> Partition<T>(IEnumerable<T> src, int num)
        {
            var enu = src.GetEnumerator();
            while (true)
            {
                var result = new List<T>(num);
                for (var i = 0; i < num; i++)
                {
                    if (!enu.MoveNext())
                    {
                        if (i > 0) yield return result;
                        yield break;
                    }
                    result.Add(enu.Current);
                }
                yield return result;
            }
        }

        public static void EnsureUsingValidDecryptionKey()
        {
            try
            {
                if (GetDecryptionKey(NewestDecryptionKeyID).Length <= 0)
                {
                    GenerateDecryptionKey();
                    return;
                }
            }
            catch (Exception)
            {
                GenerateDecryptionKey();
                return;
            }
        }
    }
}
