// ***********************************************************************
// Assembly         : SocketServer
// Author           : dykey
// Created          : 05-31-2022
//
// Last Modified By : dykey
// Last Modified On : 05-31-2022
// ***********************************************************************
// <copyright file="enums.cs" company="SocketServer">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
namespace SocketServer
{
    /// <summary>
    /// Enum Remitente
    /// </summary>
    enum Remitente
    {
        /// <summary>
        /// All clients online list
        /// </summary>
        AllClientsOnline,
        /// <summary>
        /// Create new client.
        /// </summary>
        NewClient_Linked,
        /// <summary>
        /// general message
        /// </summary>
        GeneralSvMessage,
        /// <summary>
        /// sending server dat to and from
        /// </summary>
        SendingServerDat,
        /// <summary>
        /// Client connection request
        /// </summary>
        ClientConnClosed,
        /// <summary>
        /// private message
        /// </summary>
        PrivateUsMessage,
        /// <summary>
        /// request to server
        /// </summary>
        requesttToServer
    }
    /// <summary>
    /// Enum FontColors
    /// </summary>
    enum FontColors
    {
        /// <summary>
        /// The white
        /// </summary>
        white,
        /// <summary>
        /// The cyan
        /// </summary>
        cyan,
        /// <summary>
        /// The red
        /// </summary>
        red,
        /// <summary>
        /// The blue
        /// </summary>
        blue,
        /// <summary>
        /// The green
        /// </summary>
        green
    }

}
