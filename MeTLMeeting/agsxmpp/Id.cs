/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 * Copyright (c) 2003-2012 by AG-Software 											 *
 * All Rights Reserved.																 *
 * Contact information for AG-Software is available at http://www.ag-software.de	 *
 *																					 *
 * Licence:																			 *
 * The agsXMPP SDK is released under a dual licence									 *
 * agsXMPP can be used under either of two licences									 *
 * 																					 *
 * A commercial licence which is probably the most appropriate for commercial 		 *
 * corporate use and closed source projects. 										 *
 *																					 *
 * The GNU Public License (GPL) is probably most appropriate for inclusion in		 *
 * other open source projects.														 *
 *																					 *
 * See README.html for details.														 *
 *																					 *
 * For general enquiries visit our website at:										 *
 * http://www.ag-software.de														 *
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

using System;

namespace agsXMPP
{
    public enum IdType
    {
        /// <summary>
        /// Numeric Id's are generated by increasing a long value
        /// </summary>
        Numeric,

        /// <summary>
        /// Guid Id's are unique, Guid packet Id's should be used for server and component applications,
        /// or apps which very long sessions (multiple days, weeks or years)
        /// </summary>
        Guid
    }

	/// <summary>
	/// This class takes care anout out unique Message Ids
	/// </summary>
	public class Id
	{		
        public Id()
		{			
		}

        private static long     m_id        = 0;
		private static string	m_Prefix	= "agsXMPP_";
        private static IdType   m_Type      = IdType.Numeric;

        public static IdType Type
        {
            get { return m_Type; }
#if !CF
            // readyonly on CF1
            set { m_Type = value; }
#endif
        }

#if !CF
		public static string GetNextId()		
        {
            if (m_Type == IdType.Numeric)
            {
                m_id++;
                return m_Prefix + m_id.ToString();
            }
            else
            {
                return m_Prefix + Guid.NewGuid().ToString();
            }
		}
#else
        
        // On CF 1.0 we have no GUID class, so only increasing numberical id's are supported
        // We could create GUID's on CF 1.0 with the Crypto API if we want to.
        public static string GetNextId()
        {            
            m_id++;
            return m_Prefix + m_id.ToString();
        }
#endif

        /// <summary>
		/// Reset the id counter to agsXmpp_1 again
		/// </summary>
		public static void Reset()
		{
			m_id = 0;
		}

		/// <summary>
		/// to Save Bandwidth on Mobile devices you can change the prefix
		/// null is also possible to optimize Bandwidth usage
		/// </summary>
		public static string Prefix
		{
			get { return m_Prefix; }
			set 
			{ 
				if (value == null)
					m_Prefix = "";
				else
					m_Prefix = value; 
			}
		}
	}
}