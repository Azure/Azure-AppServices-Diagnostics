"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
/* --------------------------------------------------------------------------------------------
 * Copyright (c) 2018 TypeFox GmbH (http://www.typefox.io). All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */
var ws = require("ws");
var url = require("url");
var express = require("express");
var json_server_launcher_1 = require("./json-server-launcher");
process.on('uncaughtException', function (err) {
    console.error('Uncaught Exception: ', err.toString());
    if (err.stack) {
        console.error(err.stack);
    }
});
// create the express application
var app = express();
// server the static content, i.e. index.html
app.use(express.static(__dirname));
// start the server
var server = app.listen(3000);
// create the web socket
var wss = new ws.Server({
    noServer: true,
    perMessageDeflate: false
});
server.on('upgrade', function (request, socket, head) {
    var pathname = request.url ? url.parse(request.url).pathname : undefined;
    if (pathname === '/socket') {
        wss.handleUpgrade(request, socket, head, function (webSocket) {
            var socket = {
                send: function (content) { return webSocket.send(content, function (error) {
                    if (error) {
                        throw error;
                    }
                }); },
                onMessage: function (cb) { return webSocket.on('message', cb); },
                onError: function (cb) { return webSocket.on('error', cb); },
                onClose: function (cb) { return webSocket.on('close', cb); },
                dispose: function () { return webSocket.close(); }
            };
            // launch the server when the web socket is opened
            if (webSocket.readyState === webSocket.OPEN) {
                json_server_launcher_1.launch(socket);
            }
            else {
                webSocket.on('open', function () { return json_server_launcher_1.launch(socket); });
            }
        });
    }
});
//# sourceMappingURL=server.js.map