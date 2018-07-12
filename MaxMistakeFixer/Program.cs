using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;

namespace MaxMistakeFixer
{
    class Program
    {
        //SELECT * FROM PEOPLE WHERE PEOPLE_CODE_ID='P000074179' OR PEOPLE_CODE_ID='P000074226'
        //PersonId=69700  69747
        //BEGIN TRANSACTION FixMaxMistake;
        //INSERT INTO[Campus8].[dbo].[PEOPLE] ([PEOPLE_CODE],[PEOPLE_ID],[PEOPLE_CODE_ID],[PREVIOUS_ID],[GOVERNMENT_ID],[PREV_GOV_ID],[PREFIX],[FIRST_NAME],[MIDDLE_NAME],[LAST_NAME],[SUFFIX],[NICKNAME],[PREFERRED_ADD],[BIRTH_DATE],[BIRTH_CITY],[BIRTH_STATE],[BIRTH_ZIP_CODE],[BIRTH_COUNTRY],[BIRTH_COUNTY],[DECEASED_DATE],[DECEASED_FLAG],[RELEASE_INFO],[CREATE_DATE],[CREATE_TIME],[CREATE_OPID],[CREATE_TERMINAL],[REVISION_DATE],[REVISION_TIME],[REVISION_OPID],[REVISION_TERMINAL],[ABT_JOIN],[TAX_ID],[PersonId],[PrimaryPhoneId],[Last_Name_Prefix],[LegalName]) VALUES('P','000074179','P000074179', NULL,'38870829', NULL, NULL,'Kristin Francese','M','Bayle', NULL, NULL,'Perm','1996-10-15 00:00:00.000', NULL, NULL, NULL, NULL, NULL, NULL,'N', NULL,'2015-04-08 00:00:00.000','1900-01-01 18:18:08.000','REEVESY','0001','2015-04-08 00:00:00.000','1900-01-01 18:18:08.400','REEVESY','0001','*', NULL,'69700', NULL, NULL, NULL)
        //ROLLBACK TRANSACTION FixMaxMistake;


        /********************************************************************************************************************************************************************
        -- table=PEOPLE trigger = UPDATE investigation
        -- select* FROM ABT_ACCOUNTS WHERE PEOPLE_CODE_ID='P000074179' OR PEOPLE_CODE_ID = 'P000074226'
        -- select* FROM SALUTATION
        -- select* FROM RELATIONSHIP WHERE PEOPLE_CODE_ID='P000074179' OR PEOPLE_CODE_ID = 'P000074226'
        -- select* FROM COMBINEMAILING WHERE PEOPLE_CODE_ID='P000074179' OR PEOPLE_CODE_ID = 'P000074226'
        -- select* FROM ALUMNICLASSSUMMARY WHERE PEOPLE_CODE_ID='P000074179' OR PEOPLE_CODE_ID = 'P000074226'
        -- select* FROM ADVANCENAME WHERE PEOPLE_CODE_ID='P000074179' OR PEOPLE_CODE_ID = 'P000074226'
        -- select* FROM SEVISBATCHDETAIL
        -- EXEC sp_combine_mailing just touches COMBINEMALING, we got data taken care of by table = PEOPLE trigger=DELETE script in program.cs
        -- EXEC SP_ADvanceName touches ALUMNICLASSSUMMARY.....thank GOD no data
        -- EXEC SP_ADvanceName touches ADVANCENAME ..... we got data taken care of by table = PEOPLE trigger= DELETE script in program.cs
        -- EXEC SP_ADvanceName EXEC SP_ADvanceName_2
        -- EXEC SP_ADvanceName EXEC SP_ADvanceName_2 oh my gosh its recursive so its gotta end hopefully  EXEC SP_ADvanceName
        -- exec spSevisPersonalEdit
        -- exec spSevisPersonalEdit EXECUTE spCreateSevisBatchDetail  uses RELATIONSHIP but no data see above
        -- exec spSevisPersonalEdit EXECUTE spCreateSevisBatchDetail  uses DEMOGRAPHICS we got data taken care of by table = PEOPLE trigger= DELETE script in program.cs
        -- exec spSevisPersonalEdit EXECUTE spCreateSevisBatchDetail  uses SEVISBATCHDETAIL no data thank GOD
        -- exec spSevisPersonalEdit EXECUTE spCreateSevisBatchDetail ok harmless
        -- exec spSevisPersonalEdit ok harmless
        -- Exec spAssignEpsAndCounselor touches ACADEMIC but taken care of by table = PEOPLE trigger= DELETE script in program.cs
        -- Exec spAssignEpsAndCounselor  Exec spAssignEPS touches EDUCATION ..... we got data taken care of by table = PEOPLE trigger= DELETE script in program.cs
        -- Exec spAssignEpsAndCounselor  Exec spAssignEPS touches ORGANIZATION ..... no data taken care of by table= PEOPLE trigger= DELETE script in program.cs
        -- Exec spAssignEpsAndCounselor  Exec spAssignEPS touches EPSACADEMIC ..... no data taken care of by table= PEOPLE trigger= DELETE script in program.cs
        -- Exec spAssignEpsAndCounselor  Exec spAssignEPS ok harmless
        -- Exec spAssignEpsAndCounselor  Exec spAssignCounselor ok harmless from previous investigation on EDUCATION in program.cs
        -- Exec spAssignEpsAndCounselor ok harmless 
        -- EXEC sp_uact_PEOPLE harmless cuz no code see code
        -- touches PEOPLEMETADATA but.....we got data taken care of by table = PEOPLE trigger= DELETE script in program.cs
        -- touches PersonMessage but no data
        -- table= PEOPLE trigger= UPDATE declared harmless and taken care of by table = PEOPLE trigger= DELETE below
        ****************************************************************************************************************************************************************************/

        static int DoQuery(ref StreamWriter script_writer, ref SqlConnection conn, string table_name, string people_code_id, string optionalcolumncheck = null)
        {
            string querystr = string.Format("SELECT * FROM {0} WHERE PEOPLE_CODE_ID='{1}'", table_name, people_code_id);
            if (optionalcolumncheck != null)
                querystr = string.Format("SELECT * FROM {0} WHERE {1}='{2}'", table_name, optionalcolumncheck, people_code_id);

            script_writer.WriteLine("-- " + querystr);

            SqlCommand command = new SqlCommand(querystr, conn);
            SqlDataReader dr = command.ExecuteReader();

            int resultcount = 0;
            while (dr.Read())
            {
                resultcount++;

                string column_names = "(";
                string column_values = "(";
                string temp;

                for (int ii = 0; ii < dr.FieldCount; ii++)
                {
                    if (ii == (dr.FieldCount - 1))
                        column_names += string.Format("[{0}])", dr.GetName(ii));
                    else
                        column_names += string.Format("[{0}],", dr.GetName(ii));


                    string myname = dr.GetName(ii);

                    Type mytype = dr.GetFieldType(ii);
                    if (mytype == typeof(System.String))
                    {
                        try
                        {
                            temp = dr.GetString(ii);
                        }
                        catch (SqlNullValueException ex)
                        {
                            temp = "NULL";
                        }

                        if (ii == (dr.FieldCount - 1))
                        {
                            if (temp.Equals("NULL"))
                                column_values += "NULL)";
                            else
                                column_values += string.Format("'{0}')", temp);
                        }
                        else
                        {
                            if (temp.Equals("NULL"))
                                column_values += "NULL,";
                            else
                                column_values += string.Format("'{0}',", temp);
                        }
                    }
                    else if (mytype == typeof(System.DateTime))
                    {
                        string customstr;
                        DateTime mydatetime;
                        try
                        {
                            mydatetime = dr.GetDateTime(ii);
                            customstr = mydatetime.ToString("yyyy-MM-dd HH:mm:ss.000");
                        }
                        catch (SqlNullValueException ex)
                        {
                            customstr = "NULL";
                        }

                        if (ii == (dr.FieldCount - 1))
                        {
                            if (customstr.Equals("NULL"))
                                column_values += "NULL)";
                            else
                                column_values += string.Format("'{0}')", customstr);
                        }
                        else
                        {
                            if (customstr.Equals("NULL"))
                                column_values += "NULL,";
                            else
                                column_values += string.Format("'{0}',", customstr);
                        }
                    }
                    else if (mytype == typeof(System.Int32))
                    {
                        string customstr;
                        Int32 myint;
                        try
                        {
                            myint = dr.GetInt32(ii);
                            customstr = myint.ToString();
                        }
                        catch (SqlNullValueException ex)
                        {
                            customstr = "NULL";
                        }

                        if (ii == (dr.FieldCount - 1))
                        {
                            if (customstr.Equals("NULL"))
                                column_values += "NULL)";
                            else
                                column_values += string.Format("'{0}')", customstr);
                        }
                        else
                        {
                            if (customstr.Equals("NULL"))
                                column_values += "NULL,";
                            else
                                column_values += string.Format("'{0}',", customstr);
                        }
                    }
                    else if (mytype == typeof(System.Decimal))
                    {
                        string customstr;
                        Decimal mydec;
                        try
                        {
                            mydec = dr.GetDecimal(ii);
                            customstr = mydec.ToString();
                        }
                        catch (SqlNullValueException ex)
                        {
                            customstr = "NULL";
                        }

                        if (ii == (dr.FieldCount - 1))
                        {
                            if (customstr.Equals("NULL"))
                                column_values += "NULL)";
                            else
                                column_values += string.Format("'{0}')", customstr);
                        }
                        else
                        {
                            if (customstr.Equals("NULL"))
                                column_values += "NULL,";
                            else
                                column_values += string.Format("'{0}',", customstr);
                        }
                    }
                    else if (mytype == typeof(System.Boolean))
                    {
                        string customstr;
                        bool mybool;
                        try
                        {
                            mybool = dr.GetBoolean(ii);
                        }
                        catch (SqlNullValueException ex)
                        {
                            mybool = false;
                        }

                        if (mybool)
                            customstr = "1";
                        else
                            customstr = "0";

                        if (ii == (dr.FieldCount - 1))
                        {
                            if (customstr.Equals("NULL"))
                                column_values += "NULL)";
                            else
                                column_values += string.Format("'{0}')", customstr);
                        }
                        else
                        {
                            if (customstr.Equals("NULL"))
                                column_values += "NULL,";
                            else
                                column_values += string.Format("'{0}',", customstr);
                        }
                    }
                    else if (mytype == typeof(System.Int16))
                    {
                        string customstr;
                        Int16 myint;
                        try
                        {
                            myint = dr.GetInt16(ii);
                            customstr = myint.ToString();
                        }
                        catch (SqlNullValueException ex)
                        {
                            customstr = "NULL";
                        }

                        if (ii == (dr.FieldCount - 1))
                        {
                            if (customstr.Equals("NULL"))
                                column_values += "NULL)";
                            else
                                column_values += string.Format("'{0}')", customstr);
                        }
                        else
                        {
                            if (customstr.Equals("NULL"))
                                column_values += "NULL,";
                            else
                                column_values += string.Format("'{0}',", customstr);
                        }
                    }
                    else
                    {
                        script_writer.WriteLine("mytype={0}", mytype.ToString());
                    }
                }
                script_writer.WriteLine("INSERT INTO {0} {1} VALUES {2}", table_name, column_names, column_values);
            }
            dr.Close();

            if (resultcount == 0)
                script_writer.WriteLine("-- NO INSERT INTO {0}", table_name);

            script_writer.WriteLine();
            script_writer.WriteLine();

            return resultcount;
        }






        static List<string> DoSpecialQuery_CHARGECREDIT_GET_CHARGECREDITNUMBER(ref StreamWriter script_writer, ref SqlConnection conn, string table_name, string people_code_id, string optionalcolumncheck = null)
        {
            string querystr = string.Format("SELECT CHARGECREDITNUMBER FROM {0} WHERE PEOPLE_CODE_ID='{1}'", table_name, people_code_id);
            if (optionalcolumncheck != null)
                querystr = string.Format("SELECT CHARGECREDITNUMBER FROM {0} WHERE {1}='{2}'", table_name, optionalcolumncheck, people_code_id);

            script_writer.WriteLine("-- " + querystr);

            SqlCommand command = new SqlCommand(querystr, conn);
            SqlDataReader dr = command.ExecuteReader();
            List<string> mylist = new List<string>();

            while (dr.Read())
            {
                for (int ii = 0; ii < dr.FieldCount; ii++)
                {
                    string myname = dr.GetName(ii);

                    Type mytype = dr.GetFieldType(ii);
                    if (mytype == typeof(System.Int32))
                    {
                        string customstr;
                        Int32 myint;
                        try
                        {
                            myint = dr.GetInt32(ii);
                            customstr = myint.ToString();
                        }
                        catch (SqlNullValueException ex)
                        {
                            customstr = "NULL";
                        }

                        mylist.Add(customstr);
                    }
                    else
                    {
                        throw new Exception("Forced exception expection Int32 in DoSpecialQuery_CHARGECREDIT_GET_CHARGECREDITNUMBER!!!!!");
                    }
                }
            }
            dr.Close();
            return mylist;
        }




        static void DoSpecialChargeCreditDistQuery(ref List<string> chargecreditnumber_list, ref StreamWriter script_writer, ref SqlConnection conn)
        {
            //PEOPLE_CODE_ID==P000074179  select * FROM [Campus8_ceeb].[dbo].[CHARGECREDITDIST] WHERE CHARGECREDITNUMBER = '1767535' OR CHARGECREDITNUMBER = '1767538' OR CHARGECREDITNUMBER = '1767540'
            string bigchargestr = "";
            for (int ii=0; ii < chargecreditnumber_list.Count; ii++)
            {
                bigchargestr += string.Format("CHARGECREDITNUMBER = '{0}'", chargecreditnumber_list[ii]);
                if (ii != (chargecreditnumber_list.Count - 1))
                    bigchargestr += " OR ";
            }

            string querystr = string.Format("select * FROM [Campus8_ceeb].[dbo].[CHARGECREDITDIST] WHERE {0}", bigchargestr);

            script_writer.WriteLine("-- " + querystr);

            SqlCommand command = new SqlCommand(querystr, conn);
            SqlDataReader dr = command.ExecuteReader();

            while (dr.Read())
            {

                string column_names = "(";
                string column_values = "(";
                string temp;

                for (int ii = 0; ii < dr.FieldCount; ii++)
                {
                    if (ii == (dr.FieldCount - 1))
                        column_names += string.Format("[{0}])", dr.GetName(ii));
                    else
                        column_names += string.Format("[{0}],", dr.GetName(ii));


                    string myname = dr.GetName(ii);

                    Type mytype = dr.GetFieldType(ii);
                    if (mytype == typeof(System.String))
                    {
                        try
                        {
                            temp = dr.GetString(ii);
                        }
                        catch (SqlNullValueException ex)
                        {
                            temp = "NULL";
                        }

                        if (ii == (dr.FieldCount - 1))
                        {
                            if (temp.Equals("NULL"))
                                column_values += "NULL)";
                            else
                                column_values += string.Format("'{0}')", temp);
                        }
                        else
                        {
                            if (temp.Equals("NULL"))
                                column_values += "NULL,";
                            else
                                column_values += string.Format("'{0}',", temp);
                        }
                    }
                    else if (mytype == typeof(System.DateTime))
                    {
                        string customstr;
                        DateTime mydatetime;
                        try
                        {
                            mydatetime = dr.GetDateTime(ii);
                            customstr = mydatetime.ToString("yyyy-MM-dd HH:mm:ss.000");
                        }
                        catch (SqlNullValueException ex)
                        {
                            customstr = "NULL";
                        }

                        if (ii == (dr.FieldCount - 1))
                        {
                            if (customstr.Equals("NULL"))
                                column_values += "NULL)";
                            else
                                column_values += string.Format("'{0}')", customstr);
                        }
                        else
                        {
                            if (customstr.Equals("NULL"))
                                column_values += "NULL,";
                            else
                                column_values += string.Format("'{0}',", customstr);
                        }
                    }
                    else if (mytype == typeof(System.Int32))
                    {
                        string customstr;
                        Int32 myint;
                        try
                        {
                            myint = dr.GetInt32(ii);
                            customstr = myint.ToString();
                        }
                        catch (SqlNullValueException ex)
                        {
                            customstr = "NULL";
                        }

                        if (ii == (dr.FieldCount - 1))
                        {
                            if (customstr.Equals("NULL"))
                                column_values += "NULL)";
                            else
                                column_values += string.Format("'{0}')", customstr);
                        }
                        else
                        {
                            if (customstr.Equals("NULL"))
                                column_values += "NULL,";
                            else
                                column_values += string.Format("'{0}',", customstr);
                        }
                    }
                    else if (mytype == typeof(System.Decimal))
                    {
                        string customstr;
                        Decimal mydec;
                        try
                        {
                            mydec = dr.GetDecimal(ii);
                            customstr = mydec.ToString();
                        }
                        catch (SqlNullValueException ex)
                        {
                            customstr = "NULL";
                        }

                        if (ii == (dr.FieldCount - 1))
                        {
                            if (customstr.Equals("NULL"))
                                column_values += "NULL)";
                            else
                                column_values += string.Format("'{0}')", customstr);
                        }
                        else
                        {
                            if (customstr.Equals("NULL"))
                                column_values += "NULL,";
                            else
                                column_values += string.Format("'{0}',", customstr);
                        }
                    }
                    else if (mytype == typeof(System.Boolean))
                    {
                        string customstr;
                        bool mybool;
                        try
                        {
                            mybool = dr.GetBoolean(ii);
                        }
                        catch (SqlNullValueException ex)
                        {
                            mybool = false;
                        }

                        if (mybool)
                            customstr = "1";
                        else
                            customstr = "0";

                        if (ii == (dr.FieldCount - 1))
                        {
                            if (customstr.Equals("NULL"))
                                column_values += "NULL)";
                            else
                                column_values += string.Format("'{0}')", customstr);
                        }
                        else
                        {
                            if (customstr.Equals("NULL"))
                                column_values += "NULL,";
                            else
                                column_values += string.Format("'{0}',", customstr);
                        }
                    }
                    else if (mytype == typeof(System.Int16))
                    {
                        string customstr;
                        Int16 myint;
                        try
                        {
                            myint = dr.GetInt16(ii);
                            customstr = myint.ToString();
                        }
                        catch (SqlNullValueException ex)
                        {
                            customstr = "NULL";
                        }

                        if (ii == (dr.FieldCount - 1))
                        {
                            if (customstr.Equals("NULL"))
                                column_values += "NULL)";
                            else
                                column_values += string.Format("'{0}')", customstr);
                        }
                        else
                        {
                            if (customstr.Equals("NULL"))
                                column_values += "NULL,";
                            else
                                column_values += string.Format("'{0}',", customstr);
                        }
                    }
                    else
                    {
                        script_writer.WriteLine("mytype={0}", mytype.ToString());
                    }
                }
                script_writer.WriteLine("INSERT INTO [Campus8_ceeb].[dbo].[CHARGECREDITDIST] {0} VALUES {1}", column_names, column_values);
            }
            dr.Close();

            script_writer.WriteLine();
            script_writer.WriteLine();
        }




        static void Main(string[] args)
        {
            StreamWriter script_writer = new StreamWriter(@".\FixMaxMistake.sql");
            script_writer.WriteLine("BEGIN TRANSACTION FixMaxMistake;");


            script_writer.WriteLine("DISABLE TRIGGER[dbo].[ti_people] ON [dbo].[PEOPLE];");
            script_writer.WriteLine("GO");
            script_writer.WriteLine("DISABLE TRIGGER[dbo].[tu_people] ON [dbo].[PEOPLE];");
            script_writer.WriteLine("GO");

            script_writer.WriteLine("DISABLE TRIGGER[dbo].[ti_studentfinancial] ON [dbo].[STUDENTFINANCIAL];");
            script_writer.WriteLine("GO");
            script_writer.WriteLine("DISABLE TRIGGER[dbo].[tu_studentfinancial] ON [dbo].[STUDENTFINANCIAL];");
            script_writer.WriteLine("GO");

            script_writer.WriteLine("DISABLE TRIGGER[dbo].[tiStudent] ON [dbo].[STUDENT];");
            script_writer.WriteLine("GO");
            script_writer.WriteLine("DISABLE TRIGGER[dbo].[tuStudent] ON [dbo].[STUDENT];");
            script_writer.WriteLine("GO");
            script_writer.WriteLine("DISABLE TRIGGER[dbo].[tiMailing] ON [dbo].[MAILING];");
            script_writer.WriteLine("GO");
            script_writer.WriteLine("DISABLE TRIGGER[dbo].[tuMailing] ON [dbo].[MAILING];");
            script_writer.WriteLine("GO");
            script_writer.WriteLine("DISABLE TRIGGER[dbo].[tiAddress] ON [dbo].[ADDRESS];");
            script_writer.WriteLine("GO");
            script_writer.WriteLine("DISABLE TRIGGER[dbo].[tuAddress] ON [dbo].[ADDRESS];");
            script_writer.WriteLine("GO");
            script_writer.WriteLine("DISABLE TRIGGER[dbo].[tiPeopleType] ON [dbo].[PEOPLETYPE];");
            script_writer.WriteLine("GO");
            script_writer.WriteLine("DISABLE TRIGGER[dbo].[tuPeopleType] ON [dbo].[PEOPLETYPE];");
            script_writer.WriteLine("GO");

            script_writer.WriteLine("DISABLE TRIGGER[dbo].[tiSourceDetail] ON [dbo].[SOURCEDETAIL];");
            script_writer.WriteLine("GO");
            script_writer.WriteLine("DISABLE TRIGGER[dbo].[tuSourceDetail] ON [dbo].[SOURCEDETAIL];");
            script_writer.WriteLine("GO");

            script_writer.WriteLine("DISABLE TRIGGER[dbo].[tiouStageHistory] ON [dbo].[STAGEHISTORY];");
            script_writer.WriteLine("GO");

            script_writer.WriteLine("-- FULLPARTHISTORY NO TRIGGERS");
            script_writer.WriteLine();

            script_writer.WriteLine("DISABLE TRIGGER[dbo].[ti_education] ON [dbo].[EDUCATION];");
            script_writer.WriteLine("GO");
            script_writer.WriteLine("DISABLE TRIGGER[dbo].[tu_education] ON [dbo].[EDUCATION];");
            script_writer.WriteLine("GO");

            script_writer.WriteLine("DISABLE TRIGGER[dbo].[ti_academic] ON [dbo].[ACADEMIC];");
            script_writer.WriteLine("GO");
            script_writer.WriteLine("DISABLE TRIGGER[dbo].[tu_academic] ON [dbo].[ACADEMIC];");
            script_writer.WriteLine("GO");
            script_writer.WriteLine("DISABLE TRIGGER[dbo].[tu_acadkey_change] ON [dbo].[ACADEMIC];");
            script_writer.WriteLine("GO");

            script_writer.WriteLine("DISABLE TRIGGER[dbo].[ti_transcriptdetail] ON [dbo].[TRANSCRIPTDETAIL];");
            script_writer.WriteLine("GO");
            script_writer.WriteLine("DISABLE TRIGGER[dbo].[tu_transcriptdetail] ON [dbo].[TRANSCRIPTDETAIL];");
            script_writer.WriteLine("GO");
            script_writer.WriteLine("DISABLE TRIGGER[dbo].[web3_tu_drop_notif] ON [dbo].[TRANSCRIPTDETAIL];");
            script_writer.WriteLine("GO");
            script_writer.WriteLine("DISABLE TRIGGER[dbo].[web3_tu_transcriptdetail] ON [dbo].[TRANSCRIPTDETAIL];");
            script_writer.WriteLine("GO");

            script_writer.WriteLine("-- TRANSEDUCATION NO TRIGGERS");
            script_writer.WriteLine();

            script_writer.WriteLine("DISABLE TRIGGER[dbo].[tI_TranscriptMarketing] ON [dbo].[TRANSCRIPTMARKETING];");
            script_writer.WriteLine("GO");
            script_writer.WriteLine("DISABLE TRIGGER[dbo].[tU_TranscriptMarketing] ON [dbo].[TRANSCRIPTMARKETING];");
            script_writer.WriteLine("GO");

            script_writer.WriteLine("DISABLE TRIGGER[dbo].[ti_transcriptheader] ON [dbo].[TRANSCRIPTHEADER];");
            script_writer.WriteLine("GO");

            script_writer.WriteLine("DISABLE TRIGGER[dbo].[ti_academicinterest] ON [dbo].[ACADEMICINTEREST];");
            script_writer.WriteLine("GO");
            script_writer.WriteLine("DISABLE TRIGGER[dbo].[tu_academicinterest] ON [dbo].[ACADEMICINTEREST];");
            script_writer.WriteLine("GO");

            script_writer.WriteLine("DISABLE TRIGGER[dbo].[ti_testscores] ON [dbo].[TESTSCORES];");
            script_writer.WriteLine("GO");
            script_writer.WriteLine("DISABLE TRIGGER[dbo].[tu_testscores] ON [dbo].[TESTSCORES];");
            script_writer.WriteLine("GO");

            script_writer.WriteLine("DISABLE TRIGGER[dbo].[ti_residency] ON [dbo].[RESIDENCY];");
            script_writer.WriteLine("GO");
            script_writer.WriteLine("DISABLE TRIGGER[dbo].[tu_residency] ON [dbo].[RESIDENCY];");
            script_writer.WriteLine("GO");


            script_writer.WriteLine("DISABLE TRIGGER[dbo].[ti_chargecreditdist] ON [dbo].[CHARGECREDITDIST];");
            script_writer.WriteLine("GO");
            script_writer.WriteLine("DISABLE TRIGGER[dbo].[tu_chargecreditdist] ON [dbo].[CHARGECREDITDIST];");
            script_writer.WriteLine("GO");

            script_writer.WriteLine("-- PEOPLEORGBALANCE NO TRIGGERS");
            script_writer.WriteLine();

            script_writer.WriteLine("DISABLE TRIGGER[dbo].[tiChargeCredit] ON [dbo].[CHARGECREDIT];");
            script_writer.WriteLine("GO");
            script_writer.WriteLine("DISABLE TRIGGER[dbo].[tuChargeCredit] ON [dbo].[CHARGECREDIT];");
            script_writer.WriteLine("GO");

            script_writer.WriteLine("DISABLE TRIGGER[dbo].[ti_actionschedule] ON [dbo].[ACTIONSCHEDULE];");
            script_writer.WriteLine("GO");
            script_writer.WriteLine("DISABLE TRIGGER[dbo].[tu_actionschedule] ON [dbo].[ACTIONSCHEDULE];");
            script_writer.WriteLine("GO");

            script_writer.WriteLine("DISABLE TRIGGER[dbo].[ti_demographics] ON [dbo].[DEMOGRAPHICS];");
            script_writer.WriteLine("GO");
            script_writer.WriteLine("DISABLE TRIGGER[dbo].[tu_demographics] ON [dbo].[DEMOGRAPHICS];");
            script_writer.WriteLine("GO");

            script_writer.WriteLine("DISABLE TRIGGER[dbo].[tiAddressSchedule] ON [dbo].[ADDRESSSCHEDULE];");
            script_writer.WriteLine("GO");
            script_writer.WriteLine("DISABLE TRIGGER[dbo].[tuAddressSchedule] ON [dbo].[ADDRESSSCHEDULE];");
            script_writer.WriteLine("GO");

            script_writer.WriteLine("DISABLE TRIGGER[dbo].[tuAddressHierarchyUnique] ON [dbo].[ADDRESSHIERARCHYUNIQUE];");
            script_writer.WriteLine("GO");

            script_writer.WriteLine("DISABLE TRIGGER[dbo].[ti_combinemailing] ON [dbo].[COMBINEMAILING];");
            script_writer.WriteLine("GO");
            script_writer.WriteLine("DISABLE TRIGGER[dbo].[tu_combinemailing] ON [dbo].[COMBINEMAILING];");
            script_writer.WriteLine("GO");

            script_writer.WriteLine("-- ADVANCENAME NO TRIGGERS");
            script_writer.WriteLine();

            script_writer.WriteLine("-- PFINTEGRATION NO TRIGGERS");
            script_writer.WriteLine();

            script_writer.WriteLine("-- STUDENTASSESS NO TRIGGERS");
            script_writer.WriteLine();

            script_writer.WriteLine("-- PEOPLEMETADATA NO TRIGGERS");
            script_writer.WriteLine();

            script_writer.WriteLine();
            script_writer.WriteLine();

            SqlConnection conn = new SqlConnection("Data Source=budb01;Initial Catalog=Campus8_ceeb;Integrated Security=True");
            conn.Open();


            List<string> listpeoplecodeid = new List<string>();
            listpeoplecodeid.Add("P000074179");
            listpeoplecodeid.Add("P000074226");

            for (int ii=0; ii < listpeoplecodeid.Count; ii++)
            {
                script_writer.WriteLine("-- **********************************************************************************************");
                script_writer.WriteLine("-- {0} of {1} peoplecodeid={2}", ii+1, listpeoplecodeid.Count, listpeoplecodeid[ii]);
                script_writer.WriteLine("-- **********************************************************************************************");

                string people_code_id = listpeoplecodeid[ii];

                script_writer.WriteLine("-- [Campus8_ceeb].[dbo].[ABT_ACCOUNTS] NO DATA FOUND - no touching of other tables and cannot restore given Campus8_ceeb");

                script_writer.WriteLine("-- table=STUDENTFINANCIAL checked INSERT AND DELETE triggers");
                script_writer.WriteLine("-- table=STUDENTFINANCIAL trigger=DELETE exec sp_studfin_activity ..... ok does no other table editing");
                script_writer.WriteLine("-- table=STUDENTFINANCIAL trigger=DELETE exec sp_u_studentfinancial_rollup ..... ok does no other table editing");
                script_writer.WriteLine("-- table=STUDENTFINANCIAL trigger=DELETE EXEC sp_dact_STUDENTFINANCIAL ..... ok does nothing look at code");
                script_writer.WriteLine("-- table=STUDENTFINANCIAL trigger=INSERT exec sp_studfin_activity ..... ok does no other table editing");
                script_writer.WriteLine("-- table=STUDENTFINANCIAL trigger=INSERT exec sp_u_studentfinancial_rollup ..... ok does no other table editing");
                script_writer.WriteLine("-- table=STUDENTFINANCIAL trigger=INSERT EXEC sp_iact_STUDENTFINANCIAL ..... ok does nothing look at code");
                script_writer.WriteLine("-- table=STUDENTFINANCIAL is a leaf-node it touches no other tables");

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[STUDENTFINANCIAL]", people_code_id);

                script_writer.WriteLine("-- table=STUDENT trigger=DELETE EXEC sp_dact_STUDENT  -- this does nothing looked at code");
                script_writer.WriteLine("-- table=STUDENT trigger=INSERT EXEC sp_iact_STUDENT  -- this does nothing looked at code");
                script_writer.WriteLine("-- table=STUDENT is a leaf-node it touches no other tables");

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[STUDENT]", people_code_id);

                script_writer.WriteLine("-- table=MAILING trigger=DELETE there is no trigger");
                script_writer.WriteLine("-- table=MAILING trigger=INSERT and UPDATE since these are people do not touch other people");
                script_writer.WriteLine("-- table=MAILING is a leaf-node it touches no other tables");

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[MAILING]", people_code_id, "MAILING.PEOPLE_ORG_CODE_ID");
                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[MAILING]", people_code_id, "MAILING.RECIPIENT_ID");

                script_writer.WriteLine("-- table=ADDRESS trigger=DELETE does not touch other tables");
                script_writer.WriteLine("-- table=ADDRESS trigger=INSERT does not touch other tables");
                script_writer.WriteLine("-- table=ADDRESS is a leafnode and it touches no other tables");

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[ADDRESS]", people_code_id, "ADDRESS.PEOPLE_ORG_CODE_ID");


                script_writer.WriteLine("-- table=PEOPLETYPE trigger=DELETE updates FACULTY but these guys are not in that table no worrys");
                script_writer.WriteLine("-- table=PEOPLETYPE trigger=DELETE updates CASETYPE but these guys are not in that table no worrys");
                script_writer.WriteLine("-- table=PEOPLETYPE trigger=DELETE updates MAILING but this is a leafnode reinserted above no worrys");
                script_writer.WriteLine("-- table=PEOPLETYPE trigger=DELETE EXEC sp_dact_PEOPLETYPE does nothing you can see code");
                script_writer.WriteLine("-- table=PEOPLETYPE trigger=INSERT EXEC sp_create_student just does insert into STUDENT but we got that as leafnode above");
                script_writer.WriteLine("-- table=PEOPLETYPE trigger=INSERT EXEC sp_create_mailings does insert into mailings, but we got it as leafnode above");
                script_writer.WriteLine("-- table=PEOPLETYPE trigger=INSERT EXEC sp_iact_PEOPLETYPE does nothing you can see code");

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[PEOPLETYPE]", people_code_id);

                script_writer.WriteLine("-- table=SOURCEDETAIL trigger=DELETE EXEC sp_dact_SOURCEDETAIL does nothing saw code");
                script_writer.WriteLine("-- table=SOURCEDETAIL trigger=INSERT EXEC sp_iact_SOURCEDETAIL does nothing saw code");
                script_writer.WriteLine("-- table=SOURCEDETAIL is a leafnode and it touches no other tables");

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[SOURCEDETAIL]", people_code_id);

                script_writer.WriteLine("-- table=STAGEHISTORY is a leafnode and it touches no other tables looked at all triggers");

                script_writer.WriteLine("SET IDENTITY_INSERT [Campus8_ceeb].[dbo].[STAGEHISTORY] ON");
                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[STAGEHISTORY]", people_code_id);
                script_writer.WriteLine("SET IDENTITY_INSERT [Campus8_ceeb].[dbo].[STAGEHISTORY] OFF");

                script_writer.WriteLine("-- table=FULLPARTHISTORY is a leafnode and it touches no other tables looked at all triggers");

                script_writer.WriteLine("SET IDENTITY_INSERT [Campus8_ceeb].[dbo].[FULLPARTHISTORY] ON");
                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[FULLPARTHISTORY]", people_code_id);
                script_writer.WriteLine("SET IDENTITY_INSERT [Campus8_ceeb].[dbo].[FULLPARTHISTORY] OFF");

                //EDUCATION trigger=delete touches TRANSEDUCATION ..... (ok no data found by peoplecodeid)
                //EDUCATION trigger=delete Exec spAssignEpsAndCounselor
                //EDUCATION trigger=delete Exec spAssignEpsAndCounselor  Exec spAssignEPS ORGANIZATION (no data), EDUCATION (taken care of here), ADDRESS (taken care of above leafnode), EPSACADEMIC ( taken care of no data)
                //EDUCATION trigger=delete Exec spAssignEpsAndCounselor  Exec spAssignCounselor
                //EDUCATION trigger=delete Exec spAssignEpsAndCounselor  Exec spAssignCounselor  Exec spAssignCounselorByException  just updates ACADEMIC (taken care of above)
                //EDUCATION trigger=delete Exec spAssignEpsAndCounselor  Exec spAssignCounselor  Exec spAssignCounselorByEPS just updates ACADEMIC (taken care of above)
                //EDUCATION trigger=delete Exec spAssignEpsAndCounselor  Exec spAssignCounselorByInstitution just updates ACADEMIC (taken care of above)
                //EDUCATION trigger=delete Exec spAssignEpsAndCounselor  Exec spAssignCounselor ok just updates ACADEMIC harmless so far
                //EDUCATION trigger=delete Exec spAssignEpsAndCounselor  EXEC sp_dact_EDUCATION  does nothing see code
                //EDUCATION ok declare EDUCATION a leafnode

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[EDUCATION]", people_code_id);


                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[ACADEMIC]", people_code_id);

                //TRANSCRIPTDETAIL trigger=delete 
                //TRANSCRIPTDETAIL trigger=delete STUDENTFINANCIAL leafnode
                //TRANSCRIPTDETAIL trigger=delete exec spRecalcAcademic
                //TRANSCRIPTDETAIL trigger=delete exec spRecalcAcademic  exec spUpdStudentLastPeriod  updates STUDENT but we restore as leafnode above
                //TRANSCRIPTDETAIL trigger=delete exec spRecalcAcademic  exec spCalculateClassLevel 
                //TRANSCRIPTDETAIL trigger=delete exec spRecalcAcademic  exec spCalculateClassLevel  exec spCountClassLvlCredits
                //TRANSCRIPTDETAIL trigger=delete exec spRecalcAcademic  exec spCalculateClassLevel  exec spCountClassLvlCredits  exec sp_get_gradevalues_count just does math on GRADEVALUES
                //TRANSCRIPTDETAIL trigger=delete exec spRecalcAcademic  exec spCalculateClassLevel  exec spCountClassLvlCredits  exec sp_get_gradevalues_pass_fail just does math on GRADEVALUES
                //TRANSCRIPTDETAIL trigger=delete exec spRecalcAcademic  exec spCalculateClassLevel  exec spCountClassLvlCredits  exec sp_compare_grade just does math on GRADEVALUES
                //TRANSCRIPTDETAIL trigger=delete exec spRecalcAcademic  exec spCalculateClassLevel  exec spCountClassLvlCredits  exec sp_get_repeatvalues_cum minor query on REPEATVALUES
                //TRANSCRIPTDETAIL trigger=delete exec spRecalcAcademic  exec spCalculateClassLevel  exec spCountClassLvlCredits  exec sp_get_repeatvalues_remove minor query on REPEATVALUES
                //TRANSCRIPTDETAIL trigger=delete exec spRecalcAcademic  exec spCalculateClassLevel  exec spCountClassLvlCredits ok declared harmless
                //TRANSCRIPTDETAIL trigger=delete exec spRecalcAcademic  exec spCalculateClassLevel ok declare harmless
                //TRANSCRIPTDETAIL trigger=delete exec spRecalcAcademic  exec spCalculateEnrollmentSeparation
                //TRANSCRIPTDETAIL trigger=delete exec spRecalcAcademic  exec spCalculateEnrollmentSeparation  exec sp_update_academic_sep_rollup ok just does stuff on ACADEMIC but we got it inserted above disabled triggers
                //TRANSCRIPTDETAIL trigger=delete exec spRecalcAcademic  exec spCalculateEnrollmentSeparation ok its harmless
                //TRANSCRIPTDETAIL trigger=delete exec spRecalcAcademic  exec spCalculateFullTimePartTime ok queries on TRANSCRIPTDETAIL which is this and ACADEMIC which is above no triggers and GRADEVALUES which is unchanged so harmless
                //TRANSCRIPTDETAIL trigger=delete exec spRecalcAcademic  updates ACADEMIC but that is reinserted above no change disabled queries
                //TRANSCRIPTDETAIL trigger=delete exec spRecalcAcademic  exec sp_update_academic_fullpt  again math on ACADEMIC, TRANSCRIPTDETAIL, and GRADEVALUES so harmless 
                //TRANSCRIPTDETAIL trigger=delete exec spRecalcAcademic  and finally updates ACADEMIC which is unchanged above so harmless
                //TRANSCRIPTDETAIL trigger=delete exec dbo.spUpdSponsorSectionTallies updates SECTIONS but this not important for us
                //TRANSCRIPTDETAIL tirgger=delete exec dbo.spUpdateAllTally  ok updates SECTIONS but this not important for us
                //TRANSCRIPTDETAIL tirgger=delete changes GRADECHANGE but no data
                //TRANSCRIPTDETAIL tirgger=delete changes TRANATTENDANCE but no data
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_overall 
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_overall  exec sp_calc_gpa_check_repeat
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_overall  exec sp_calc_gpa_check_repeat   exec sp_calc_gpa_new_cum ..... accesses TRANSCRIPTGPA but no data in TRANSCRIPTGPA
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_overall  exec sp_calc_gpa_check_repeat   exec sp_get_gradevalues_pass_fail just does math on GRADEVALUES
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_overall  exec sp_calc_gpa_check_repeat   exec sp_compare_grade just does math on GRADEVALUES
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_overall  exec sp_calc_gpa_check_repeat   exec sp_get_repeatvalues_sess just does query on REPEATVALUES
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_overall  exec sp_calc_gpa_check_repeat   exec sp_get_repeatvalues_cum  just does stuff on REPEATVALUES
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_overall  exec sp_calc_gpa_check_repeat   exec sp_get_repeatvalues_remove just does stuff on REPEATVALUES
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_overall  exec sp_calc_gpa_check_repeat   exec sp_calc_gpa_future
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_overall  exec sp_calc_gpa_check_repeat   exec sp_calc_gpa_future  exec sp_calc_gpa_2
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_overall  exec sp_calc_gpa_check_repeat   exec sp_calc_gpa_future  exec sp_calc_gpa_2   exec sp_truncate just simple stuff
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_overall  exec sp_calc_gpa_check_repeat   exec sp_calc_gpa_future  exec sp_calc_gpa_2  ok harmless
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_overall  exec sp_calc_gpa_check_repeat   exec sp_calc_gpa_future ok harmless
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_overall  exec sp_calc_gpa_check_repeat   exec sp_get_gradevalues_count  just does math on GRADEVALUES
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_overall  exec sp_calc_gpa_check_repeat   exec sp_calc_gpa_2  ok harmless see above
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_overall  exec sp_calc_gpa_check_repeat   exec sp_calc_gpa_future ok harmless see above
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_overall  exec sp_calc_gpa_check_repeat  ok harmless

                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_overall  exec sp_calc_gpa_1 
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_overall  exec sp_calc_gpa_1  exec sp_calc_gpa_new_cum  harmless see above
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_overall  exec sp_calc_gpa_1  exec sp_get_gradevalues_count  harmless see above
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_overall  exec sp_calc_gpa_1  exec sp_get_repeatvalues_cum   harmless see above
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_overall  exec sp_calc_gpa_1  exec sp_get_repeatvalues_sess  harmless see above
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_overall  exec sp_calc_gpa_1  exec sp_truncate harmless see above
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_overall  exec sp_calc_gpa_1  harmless
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_overall  exec sp_create_new_term_gpa ok harmless just on TRANSCRIPTGPA
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_overall  exec sp_calc_gpa_1  harmless
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_overall  exec sp_calc_gpa_future
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_overall  exec sp_calc_gpa_future  exec sp_get_gradevalues_count see above harmless
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_overall  exec sp_calc_gpa_future  exec sp_get_repeatvalues_cum  harmless see above   
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_overall  exec sp_calc_gpa_future  exec sp_calc_gpa_2  ok harmless see above
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_overall  exec sp_calc_gpa_future  harmless ok finish harmless
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_overall ok its harmless
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_degree
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_degree exec sp_calc_gpa_check_repeat ok harmless see above
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_degree exec sp_calc_gpa_1  harmless see above
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_degree exec sp_create_new_term_gpa ok harmless see above
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_degree exec sp_calc_gpa_1  harmless see above
                //TRANSCRIPTDETAIL tirgger=delete exec sp_calc_gpa_degree exec sp_calc_gpa_future  harmless ok finish harmless
                //TRANSCRIPTDETAIL tirgger=delete exec sp_stddegreqevent_del_trandet 
                //TRANSCRIPTDETAIL tirgger=delete exec sp_stddegreqevent_del_trandet ok this touches STDDEGREQEVENT but no data
                //TRANSCRIPTDETAIL tirgger=delete exec sp_stddegreqevent_del_trandet  ok harmless
                //TRANSCRIPTDETAIL tirgger=delete ok touches TRANSCRIPTCOMMENT ok no data
                //TRANSCRIPTDETAIL tirgger=delete ok touches TRANSCRIPTGRADING ok no data
                //TRANSCRIPTDETAIL tirgger=delete ok touches TRANSCRIPTMARKETING ..... there is data its below
                //TRANSCRIPTDETAIL tirgger=delete exec sp_comp_remove_from_student
                //TRANSCRIPTDETAIL tirgger=delete exec sp_comp_remove_from_student touches table=transcompgroupevent no data thank God
                //TRANSCRIPTDETAIL tirgger=delete exec sp_comp_remove_from_student touches table=Transcompgroup  no data thank GOD
                //TRANSCRIPTDETAIL tirgger=delete exec sp_comp_remove_from_student ok harmless
                //TRANSCRIPTDETAIL tirgger=delete exec spProcessRegChangeMembership
                //TRANSCRIPTDETAIL tirgger=delete exec spProcessRegChangeMembership  exec spInsSectionWebMembershipRequest  touches SectionWebMembershipRequest no data thank GOD
                //TRANSCRIPTDETAIL tirgger=delete exec spProcessRegChangeMembership ok harmless for us
                //TRANSCRIPTDETAIL tirgger=delete exec dbo.spDelRegistrationOverride touches table RegistrationOverride thank GOD no data
                //TRANSCRIPTDETAIL tirgger=delete EXEC sp_dact_TRANSCRIPTDETAIL does nothing
                //TRANSCRIPTDETAIL ok declaring leafnode

                script_writer.WriteLine("SET IDENTITY_INSERT[Campus8_ceeb].[dbo].[TRANSCRIPTDETAIL] ON");
                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[TRANSCRIPTDETAIL]", people_code_id);
                script_writer.WriteLine("SET IDENTITY_INSERT[Campus8_ceeb].[dbo].[TRANSCRIPTDETAIL] OFF");




                //TRANSEDUCATION got data but no triggers therefore leafnode

                script_writer.WriteLine("SET IDENTITY_INSERT [Campus8_ceeb].[dbo].[TRANSEDUCATION] ON");
                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[TRANSEDUCATION]", people_code_id);
                script_writer.WriteLine("SET IDENTITY_INSERT [Campus8_ceeb].[dbo].[TRANSEDUCATION] OFF");

                //TRANSCRIPTMARKETING trigger=delete 
                //TRANSCRIPTMARKETING trigger=delete sp_dact_TRANSCRIPTMARKETING code does nothing
                //TRANSCRIPTMARKETING is a leafnode and we got data

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[TRANSCRIPTMARKETING]", people_code_id);

                //TRANSCRIPTHEADER trigger=delete
                //TRANSCRIPTHEADER trigger=delete  touches TRANSCRIPTDETAIL leafnode above
                //TRANSCRIPTHEADER trigger=delete  touches TRANSCRIPTDEGREE no data 
                //TRANSCRIPTHEADER trigger=delete  touches TRANSCRIPTHONORS no data 
                //TRANSCRIPTHEADER trigger=delete  touches TRANSCRIPTGPA no data 
                //TRANSCRIPTHEADER trigger=delete  touches TRANSCRIPTAWARD no data 
                //TRANSCRIPTHEADER trigger=delete  touches TRANSEDUCATION leafnode above
                //TRANSCRIPTHEADER is a leafnode now

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[TRANSCRIPTHEADER]", people_code_id);


                script_writer.WriteLine("-- ********************************************************************************");
                script_writer.WriteLine("-- From ACADEMIC triggers investigation:");
                script_writer.WriteLine("-- ********************************************************************************");
                script_writer.WriteLine("-- PEOPLETYPE leafnode above");
                script_writer.WriteLine("-- SOURCEDETAIL leafnode above");
                script_writer.WriteLine("-- STUDENT leafnode above");
                script_writer.WriteLine("-- STUDENTFINANCIAL leafnode above");
                script_writer.WriteLine("-- STAGEHISTORY leafnode above");
                script_writer.WriteLine("-- FULLPARTHISTORY leafnode above");
                script_writer.WriteLine("-- TRANSEDUCATION leafnode above");
                script_writer.WriteLine("-- TRANSCRIPTDETAIL leafnode above");
                script_writer.WriteLine("-- TRANSCRIPTMARKETING leafnode above");
                script_writer.WriteLine("-- TRANSCRIPTHEADER leafnode above");


                script_writer.WriteLine("-- RegistrationOverride ..... ok no data found on PersonId in ceeb or test");
                script_writer.WriteLine("-- EPSACADEMIC ..... ok no data found on peoplecodeid in ceeb or test");
                script_writer.WriteLine("-- ALUMNICLASS ..... ok no data found on peoplecodeid in ceeb or test");
                script_writer.WriteLine("-- ALUMNIDEGREE ..... ok no data found on peoplecodeid in ceeb or test");
                script_writer.WriteLine("-- ORGANIZATION ..... ok no data found on peoplecodeid in ceeb or test");
                script_writer.WriteLine("-- TermLevelCreditLimit ..... ok no data found on peoplecodeid in ceeb or test");
                script_writer.WriteLine("-- GRADECHANGE ..... ok no data found on peoplecodeid in ceeb or test");
                script_writer.WriteLine("-- STDDEGREQEVENT ..... ok no data found on peoplecodeid in ceeb or test");

                script_writer.WriteLine("-- TRANATTENDANCE ..... ok no data found on peoplecodeid in ceeb or test");
                script_writer.WriteLine("-- TRANSEDUCATION ..... ok no data found on peoplecodeid in ceeb or test");

                script_writer.WriteLine("-- TRANSCRIPTGRADING ..... ok no data found on peoplecodeid in ceeb or test");
                script_writer.WriteLine("-- TRANSCRIPTSOURCEDISCOUNT ..... ok no data found on peoplecodeid in ceeb or test");
                script_writer.WriteLine("-- TRANSCRIPTPENDING ..... ok no data found on peoplecodeid in ceeb or test");
                script_writer.WriteLine("-- TRANSCRIPTGPA ..... ok no data found on peoplecodeid in ceeb or test");
                script_writer.WriteLine("-- TRANSCRIPTCOMMENT ..... ok no data found on peoplecodeid in ceeb or test");
                script_writer.WriteLine("-- TRANSCRIPTAWARD ..... ok no data found on peoplecodeid in ceeb or test");
                script_writer.WriteLine("-- TRANSCRIPTHONORS ..... ok no data found on peoplecodeid in ceeb or test");
                script_writer.WriteLine("-- TRANSCRIPTDEGREE ..... ok no data found on peoplecodeid in ceeb or test");
                
                script_writer.WriteLine("-- transcompgroupevent ..... ok no data found on peoplecodeid in ceeb or test");
                script_writer.WriteLine("-- transcompgroup ..... ok no data found on peoplecodeid in ceeb or test");

                script_writer.WriteLine("-- [SectionWebMembershipRequest] ..... ok no data found on peoplecodeid in ceeb or test");

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[ACTIVITY]", people_code_id);

                script_writer.WriteLine("-- table=SOURCEDETAIL taken care of by trigger in table=ACADEMIC");




                script_writer.WriteLine("-- table=ACADEMICINTEREST ..... we found data");
                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[ACADEMICINTEREST]", people_code_id);
                script_writer.WriteLine("-- table=ACADEMICINTEREST trigger=DELETE");
                script_writer.WriteLine("-- table=ACADEMICINTEREST trigger=DELETE  EXEC sp_u_academicinterest touches ACAINTERESTSUM ..... no data thank GOD");
                script_writer.WriteLine("-- table=ACADEMICINTEREST trigger=DELETE  EXEC sp_dact_ACADEMICINTEREST ..... no code");
                script_writer.WriteLine("-- table=ACADEMICINTEREST ..... ok its a leafnode");





                script_writer.WriteLine("-- table=TRANSCRIPTHEADER taken care of by trigger in table=ACADEMIC");

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[STDDEGREQ]", people_code_id);




                script_writer.WriteLine("-- table=TESTSCORES ..... we found data");
                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[TESTSCORES]", people_code_id);
                script_writer.WriteLine("-- table=TESTSCORES trigger=DELETE");
                script_writer.WriteLine("-- table=TESTSCORES trigger=DELETE  EXEC sp_Total_Test_Score ..... ok this just plays with itself so no harm for now so far still checking this trigger");
                script_writer.WriteLine("-- table=TESTSCORES trigger=DELETE  EXEC sp_dact_TESTSCORES ..... code does nothing look at it");
                script_writer.WriteLine("-- table=TESTSCORES is a leafnode");



                script_writer.WriteLine("-- table=STUDENTFINANCIAL already done it is a leaf-node table");





                script_writer.WriteLine("-- table=RESIDENCY ..... we found data");
                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[RESIDENCY]", people_code_id);
                script_writer.WriteLine("-- table=RESIDENCY trigger=DELETE  exec sp_update_residency_rollup ..... ok just plays with itself harmless for now");
                script_writer.WriteLine("-- table=RESIDENCY trigger=DELETE  exec sp_u_residency_activity ..... ok just plays with itself harmless for now");
                script_writer.WriteLine("-- table=RESIDENCY trigger=DELETE  EXEC sp_dact_RESIDENCY ..... does nothing see code");
                script_writer.WriteLine("-- table=RESIDENCY is a leafnode");




                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[PERCONTRACT]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[EVENTPER]", people_code_id, "EVENTPER.PERSON_CODE_ID");

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[SECTIONPER]", people_code_id, "SECTIONPER.PERSON_CODE_ID");

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[STATEMENTLINE]", people_code_id, "STATEMENTLINE.PERSON_CODE_ID");

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[CASHRECEIPT]", people_code_id, "CASHRECEIPT.PEOPLE_ORG_CODE_ID");

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[STATEMENTHEADER]", people_code_id, "STATEMENTHEADER.PEOPLE_ORG_CODE_ID");

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[CASHRECEIPT]", people_code_id, "CASHRECEIPT.PEOPLE_ORG_CODE_ID");




                /******************************************************************************************************************************************************************
                -- select * FROM CHARGECREDITDIST WHERE CHARGECREDITNUMBER='1767535' OR CHARGECREDITNUMBER='1767538' OR CHARGECREDITNUMBER='1767540'
                -- select * FROM CHARGECREDITLINE WHERE CHARGECREDITNUMBER='1767535' OR CHARGECREDITNUMBER='1767538' OR CHARGECREDITNUMBER='1767540'
                -- select * FROM ChargeCreditScholarshipDetail WHERE ChargeNumberSource ='1767535' OR ChargeNumberSource='1767538' OR ChargeNumberSource='1767540'
                -- select * FROM ChargeCreditScholarship WHERE ScholarshipCreditNumber ='1767535' OR ScholarshipCreditNumber='1767538' OR ScholarshipCreditNumber='1767540'
                -- select * FROM PEOPLEORGBALANCE WHERE PEOPLE_ORG_CODE_ID='P000074179' OR PEOPLE_ORG_CODE_ID='P000074226'
                -- select * FROM FINAIDLINK WHERE CHARGECREDITNUMBER='1767535' OR CHARGECREDITNUMBER='1767538' OR CHARGECREDITNUMBER='1767540'
                -- select * FROM DeletedChargeCredit WHERE CHARGECREDITNUMBER='1767535' OR CHARGECREDITNUMBER='1767538' OR CHARGECREDITNUMBER='1767540'
                -- select * FROM DeletedChargeCredit WHERE PEOPLEORGCODEID='P000074179' OR PEOPLEORGCODEID='P000074226'
                *************************************************************************************************************************************************************************/
                script_writer.WriteLine("-- table=CHARGECREDIT ..... we found data");
               
                script_writer.WriteLine("-- table=CHARGECREDIT  trigger=DELETE  touches CHARGECREDITPAYMENT ..... thank GOD no data");
                script_writer.WriteLine("-- table=CHARGECREDIT  trigger=DELETE  touches CHARGECREDITCOURSE ..... thank GOD no data based on CHARGECREDITNUMBER");
                script_writer.WriteLine("-- table=CHARGECREDIT  trigger=DELETE  touches ADJUSTMENTPAYOUT ..... thank GOD no data based on CHARGECREDITNUMBER");

                script_writer.WriteLine("-- table=CHARGECREDIT  trigger=DELETE  touches CHARGECREDITDIST ..... ok there is data based on CHARGECREDITNUMBER");

                script_writer.WriteLine("-- table=CHARGECREDIT  trigger=DELETE  touches CHARGECREDITLINE ..... thank GOD no data based on CHARGECREDITNUMBER");
                script_writer.WriteLine("-- table=CHARGECREDIT  trigger=DELETE  touches ChargeCreditScholarshipDetail ..... thank GOD no data based on CHARGECREDITNUMBER");
                script_writer.WriteLine("-- table=CHARGECREDIT  trigger=DELETE  touches ChargeCreditScholarship ..... thank GOD no data based on CHARGECREDITNUMBER");

                script_writer.WriteLine("-- table=CHARGECREDIT  trigger=DELETE  exec spPeopleOrgBalanceChange touches PeopleOrgBalance ..... and there is data");

                script_writer.WriteLine("-- table=CHARGECREDIT  trigger=DELETE  touches FINAIDLINK ..... thank GOD no data based on CHARGECREDITNUMBER");
                script_writer.WriteLine("-- table=CHARGECREDIT  trigger=DELETE  exec dbo.spInsDeletedChargeCredit touches DeletedChargeCredit ..... thank GOD no data based on anything");
                script_writer.WriteLine("-- table=CHARGECREDIT  trigger=DELETE  EXEC sp_dact_CHARGECREDIT ..... no code ok we done");

                script_writer.WriteLine("-- table=CHARGECREDIT ..... ok subtables with data");
                script_writer.WriteLine("-- CHARGECREDITDIST trigger=DELETE exec sp_paymentplan_balance touches table=Paymentplan ..... thank GOD no data based on anything");


                script_writer.WriteLine("-- table=CHARGECREDIT is a special leafnode because its query is not on peoplecodeid but on CHARGECREDITNUMBER");

                int chargecreditrows = DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[CHARGECREDIT]", people_code_id, "CHARGECREDIT.PEOPLE_ORG_CODE_ID");
                if (chargecreditrows > 0)
                {
                    List<string> chargecreditnumber_list = DoSpecialQuery_CHARGECREDIT_GET_CHARGECREDITNUMBER(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[CHARGECREDIT]", people_code_id, "CHARGECREDIT.PEOPLE_ORG_CODE_ID");
                    DoSpecialChargeCreditDistQuery(ref chargecreditnumber_list, ref script_writer, ref conn);
                }



                script_writer.WriteLine("-- table PeopleOrgBalance is a leafnode because there is no triggers for it");
                script_writer.WriteLine("SET IDENTITY_INSERT [Campus8_ceeb].[dbo].[PEOPLEORGBALANCE] ON");
                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[PEOPLEORGBALANCE]", people_code_id, "PEOPLEORGBALANCE.PEOPLE_ORG_CODE_ID");
                script_writer.WriteLine("SET IDENTITY_INSERT [Campus8_ceeb].[dbo].[PEOPLEORGBALANCE] OFF");







                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[PAYMENTPLAN]", people_code_id, "PAYMENTPLAN.PEOPLE_ORG_CODE_ID");

                script_writer.WriteLine("-- table=PEOPLEORGBALANCE already done it is a leaf-node table");

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[ABT_USERS]", people_code_id);

                script_writer.WriteLine("-- NO NEED TO UPDATE ACADEMIC.ADVISOR COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();

                script_writer.WriteLine("-- NO NEED TO UPDATE ACADEMIC.COUNSELOR COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();

                script_writer.WriteLine("-- NO NEED TO UPDATE BUILDING COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();



                script_writer.WriteLine("-- table=ACTIONSCHEDULE ..... we found data");
                script_writer.WriteLine("-- table=ACTIONSCHEDULE trigger=DELETE investigation");
                script_writer.WriteLine("-- table=ACTIONSCHEDULE trigger=DELETE EXEC sp_dact_ACTIONSCHEDULE does no code see code");
                script_writer.WriteLine("-- table=ACTIONSCHEDULE declared to be leafnode");

                script_writer.WriteLine("SET IDENTITY_INSERT [Campus8_ceeb].[dbo].[ACTIONSCHEDULE] ON");
                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[ACTIONSCHEDULE]", people_code_id, "ACTIONSCHEDULE.PEOPLE_ORG_CODE_ID");
                script_writer.WriteLine("SET IDENTITY_INSERT [Campus8_ceeb].[dbo].[ACTIONSCHEDULE] OFF");


                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[NOTES]", people_code_id, "NOTES.PEOPLE_ORG_CODE_ID");

                script_writer.WriteLine("-- table=EDUCATION already done it is a leaf-node table");

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[EMPLOYMENT]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[SALUTATION]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[PEOPLEORGATTRIBUTES]", people_code_id, "PEOPLEORGATTRIBUTES.PEOPLE_ORG_CODE_ID");

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[PEOPLEFORMERNAME]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[PEOPLEGENERAL]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[FACULTY]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[PLEDGE]", people_code_id, "PLEDGE.PEOPLE_ORG_CODE_ID");

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[PRDTRACKING]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[GIFTPLEDGE]", people_code_id, "GIFTPLEDGE.PEOPLE_ORG_CODE_ID");

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[GIFTCREDITDESIGNATION]", people_code_id, "GIFTCREDITDESIGNATION.PEOPLE_ORG_CODE_ID");

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[GIFTCREDITDESIGNATION]", people_code_id, "GIFTCREDITDESIGNATION.ASSOCIATED_ID");

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[GIFTCREDITDETAIL]", people_code_id, "GIFTCREDITDETAIL.ASSOCIATED_ID");

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[GIVINGSUMMARY]", people_code_id, "GIVINGSUMMARY.PEOPLE_ORG_CODE_ID");

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[GIFTCLUB]", people_code_id, "GIFTCLUB.PEOPLE_ORG_CODE_ID");

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[GIFTDEDICATIONS]", people_code_id, "GIFTDEDICATIONS.PEOPLE_ORG_CODE_ID");

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[GIFTDEFERRED]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[GIFTNONCASH]", people_code_id, "GIFTNONCASH.PEOPLE_ORG_CODE_ID");

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[DEDICATIONS]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[Government]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[GOVTFINANCIAL]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[EXCHANGEVISITOR]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[GovernmentNotes]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[IMMUNIZATION]", people_code_id);

                script_writer.WriteLine("-- table=PEOPLETYPE already done it is a leaf-node table");

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[PUBLICRELATIONS]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[RELATIONSHIP]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[RELATIONSHIP]", people_code_id, "RELATIONSHIP.RELATION_CODE_ID");

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[SPOUSE]", people_code_id);

                script_writer.WriteLine("-- table=MAILING already done it is a leaf-node table");

                script_writer.WriteLine("-- table=[STOPLIST] no data checked by hand");

                script_writer.WriteLine("-- table=STUDENT already done it is a leaf-node table");



                script_writer.WriteLine("-- table=DEMOGRAPHICS ..... we found data");
                script_writer.WriteLine("-- table=DEMOGRAPHICS trigger=DELETE investigation");
                script_writer.WriteLine("-- table=DEMOGRAPHICS trigger=DELETE  exec sp_update_demographics_rollup just touches DEMOGRAPHICS so harmless for now");
                script_writer.WriteLine("-- table=DEMOGRAPHICS trigger=DELETE  exec sp_u_demographics_activity just touches DEMOGRAPHICS so harmless for now");
                script_writer.WriteLine("-- table=DEMOGRAPHICS trigger=DELETE  EXEC sp_dact_DEMOGRAPHICS does no code so harmless");
                script_writer.WriteLine("-- table=DEMOGRAPHICS declared to be leafnode");

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[DEMOGRAPHICS]", people_code_id);









                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[DISABILITY]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[DISABLEREQUIRE]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[PEOPLEEMERGENCY]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[ASSOCIATION]", people_code_id, "ASSOCIATION.PEOPLE_ORG_CODE_ID");

                script_writer.WriteLine("-- table=ADDRESS already done it is a leaf-node table");


                script_writer.WriteLine("-- table=ADDRESSSCHEDULE ..... we found data declared to be leafnode because it has no delete trigger");
                script_writer.WriteLine("SET IDENTITY_INSERT [Campus8_ceeb].[dbo].[ADDRESSSCHEDULE] ON");
                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[ADDRESSSCHEDULE]", people_code_id, "ADDRESSSCHEDULE.PEOPLE_ORG_CODE_ID");
                script_writer.WriteLine("SET IDENTITY_INSERT [Campus8_ceeb].[dbo].[ADDRESSSCHEDULE] OFF");


                script_writer.WriteLine("-- table=ADDRESSHIERARCHYUNIQUE ..... we found data declared to be leafnode because it has no delete trigger");
                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[ADDRESSHIERARCHYUNIQUE]", people_code_id, "ADDRESSHIERARCHYUNIQUE.PEOPLE_ORG_CODE_ID");



                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[ALUMNICLASS]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[ALUMNICLASSSUMMARY]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[CASETYPE]", people_code_id, "CASETYPE.PEOPLE_ORG_CODE_ID");





                script_writer.WriteLine("-- table=COMBINEMAILING ..... we found data");
                script_writer.WriteLine("-- table=COMBINEMAILING trigger=DELETE investigation");
                script_writer.WriteLine("-- table=COMBINEMAILING EXEC SP_ADvanceName ..... ok investigated this sp previously its harmless for us");
                script_writer.WriteLine("-- table=COMBINEMAILING declaring leafnode");

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[COMBINEMAILING]", people_code_id);










                script_writer.WriteLine("-- table=ADVANCENAME ..... we found data declared to be leafnode because it has no delete trigger");
                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[ADVANCENAME]", people_code_id);






                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[ADVPEOPLEFINANCE]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[SOLICITORINFO]", people_code_id, "SOLICITORINFO.PEOPLE_ORG_CODE_ID");

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[ORGANIZATIONCONTACT]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[PROSPECTRATING]", people_code_id, "PROSPECTRATING.PEOPLE_ORG_CODE_ID");

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[AFFILIATIONS]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[INDIVIDUALMATCHING]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[INDIVIDUALMATCHING]", people_code_id, "INDIVIDUALMATCHING.ASSOCIATED_ID");

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[TELECOMMUNICATIONS]", people_code_id, "TELECOMMUNICATIONS.PEOPLE_ORG_CODE_ID");

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[VOLUNTEERINTEREST]", people_code_id, "VOLUNTEERINTEREST.PEOPLE_ORG_CODE_ID");




                script_writer.WriteLine("-- table=PFINTEGRATION ..... we found data declared to be leafnode because it has no delete trigger");
                script_writer.WriteLine("SET IDENTITY_INSERT [Campus8_ceeb].[dbo].[PFINTEGRATION] ON");
                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[PFINTEGRATION]", people_code_id);
                script_writer.WriteLine("SET IDENTITY_INSERT [Campus8_ceeb].[dbo].[PFINTEGRATION] OFF");




                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[PFIPROCESSID]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[USERDEFINEDIND]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[TRANSCRIPTREQUEST]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[TRANSCRIPTGRADING]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[GRADEMAPPING]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[WEBREGISTRATIONID]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[NSLCDETAIL]", people_code_id);

                script_writer.WriteLine("-- table=TRANSCRIPTMARKETING already done it is a leaf-node table");

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[TRANSCRIPTSOURCEDISCOUNT]", people_code_id);


                script_writer.WriteLine("-- NO DATA IN VIOLATIONS COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();


                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[TranAttendance]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[TranAttendanceSum]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[VIOLATIONSSUMMARY]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[TranAttendanceSum]", people_code_id);

                script_writer.WriteLine("-- NO DATA IN SPONSORSTUDENTS COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();

                script_writer.WriteLine("-- NO DATA IN SPONSORAGREEMENT COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();




                script_writer.WriteLine("-- table=StudentAssess ..... we found data declared to be leafnode because it has no delete trigger");
                script_writer.WriteLine("SET IDENTITY_INSERT [Campus8_ceeb].[dbo].[StudentAssess] ON");
                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[StudentAssess]", people_code_id);
                script_writer.WriteLine("SET IDENTITY_INSERT [Campus8_ceeb].[dbo].[StudentAssess] OFF");








                script_writer.WriteLine("-- NO DATA IN MEDIARIGHTS COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();





                script_writer.WriteLine("-- table=PeopleMetaData ..... we found data declared to be leafnode because it has no delete trigger");
                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[PeopleMetaData]", people_code_id);






                script_writer.WriteLine("-- NO DATA IN RATING COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[WAITLIST]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[INTERESTLEVELS]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[INTERESTLEVELSHISTORY]", people_code_id);

                script_writer.WriteLine("-- NO DATA IN transcompgroup COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();

                script_writer.WriteLine("-- NO DATA IN transcompgroupevent COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();

                script_writer.WriteLine("-- NO DATA IN transcompetency COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();

                script_writer.WriteLine("-- NO DATA IN transcompsign COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();

                script_writer.WriteLine("-- NO DATA IN transcomptasksign COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();

                script_writer.WriteLine("-- NO DATA IN transcompcatnotes COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[COMPARISONRESULTS]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[COMPARISONRESULTS]", people_code_id, "COMPARISONRESULTS.PEOPLE_CODE_ID2");


                script_writer.WriteLine("-- NO DATA IN REGISTRATIONPERMISSION COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();


                script_writer.WriteLine("-- NO DATA IN COUNSELOREXCEPTIONS COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();

                script_writer.WriteLine("-- NO DATA IN EPSCOUNSELORLINK COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();

                script_writer.WriteLine("-- NO DATA IN EPSACADEMIC COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();

                script_writer.WriteLine("-- NO DATA IN SPONSORSTUDENTWAIVER COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();

                script_writer.WriteLine("-- NO DATA IN SPONSORCHANGES COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();

                script_writer.WriteLine("-- NO DATA IN TRANSACTIONS COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[PEOPLESCHOLARSHIP]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[PEOPLESCHOLARSHIPREQS]", people_code_id);

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[PEOPLESCHOLARSHIPNOTES]", people_code_id);

                script_writer.WriteLine("-- table=FULLPARTHISTORY already done it is a leaf-node table");

                script_writer.WriteLine("-- NO DATA IN StudentProxy COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();

                script_writer.WriteLine("-- NO DATA IN StudentProxyRequest COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();

                script_writer.WriteLine("-- NO DATA IN StudentProxyHistory COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();

                script_writer.WriteLine("-- NO DATA IN AddressApprovalRequest COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();

                script_writer.WriteLine("-- NO DATA IN Application COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();

                script_writer.WriteLine("-- NO DATA IN Inquiry COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();

                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[Reported1098TInformation]", people_code_id);

                script_writer.WriteLine("-- NO DATA IN BlockWebRegGroups COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();

                script_writer.WriteLine("-- NO DATA IN BlockWebRegGroupSections COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();

                script_writer.WriteLine("-- NO DATA IN BlockWebRegRules COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();

                script_writer.WriteLine("-- NO DATA IN BlockWebRegRuleGroups COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();

                script_writer.WriteLine("-- NO DATA IN BlockWebRegisteredPeople COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();

                script_writer.WriteLine("-- NO DATA IN InvoiceHeader COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();

                script_writer.WriteLine("-- NO DATA IN InvoicePreferredTaxpayer COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();

                script_writer.WriteLine("-- NO DATA IN SharedAdvisee COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();

                script_writer.WriteLine("-- NO DATA IN SharedAdviseeHistory COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();

                script_writer.WriteLine("-- NO DATA IN AssignmentTemplateShare COULD NOT FIND ROWS");
                script_writer.WriteLine();
                script_writer.WriteLine();

                script_writer.WriteLine("-- NO DATA IN DeletedChargeCredit COULD NOT FIND ROWS with those peoplecodeid");
                script_writer.WriteLine();
                script_writer.WriteLine();




                script_writer.WriteLine("SET IDENTITY_INSERT [Campus8_ceeb].[dbo].[PEOPLE] ON");
                DoQuery(ref script_writer, ref conn, "[Campus8_ceeb].[dbo].[PEOPLE]", people_code_id);
                script_writer.WriteLine("SET IDENTITY_INSERT [Campus8_ceeb].[dbo].[PEOPLE] OFF");


            }


            conn.Close();
            script_writer.Close();

        }
    }
}
