"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
/* --------------------------------------------------------------------------------------------
 * Copyright (c) 2018 TypeFox GmbH (http://www.typefox.io). All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */
var fs = require("fs");
var rpc = require("vscode-ws-jsonrpc");
var server = require("vscode-ws-jsonrpc/lib/server");
var vscode_languageserver_1 = require("vscode-languageserver");
function launch(socket) {
    var reader = new rpc.WebSocketMessageReader(socket);
    var writer = new rpc.WebSocketMessageWriter(socket);
    // start the language server as an external process
    var socketConnection = server.createConnection(reader, writer, function () { return socket.dispose(); });
    var serverConnection = server.createServerProcess('LSP', '/opt/omnisharp/run', ['-lsp']);
    server.forward(socketConnection, serverConnection, function (message) {
        if (rpc.isRequestMessage(message)) {
            if (message.method === vscode_languageserver_1.InitializeRequest.type.method) {
                var initializeParams = message.params;
                initializeParams.processId = process.pid;
            }
        }
        if (rpc.isNotificationMessage(message)) {
            switch (message.method) {
                case vscode_languageserver_1.DidOpenTextDocumentNotification.type.method: {
                    var didOpenParams = message.params;
                    var uri = didOpenParams.textDocument.uri;
                    var text = didOpenParams.textDocument.text;
                    if (uri)
                        fs.writeFileSync(uri.replace('file://', ''), text);
                    break;
                }
            }
        }
        return message;
    });
}
exports.launch = launch;
//# sourceMappingURL=json-server-launcher.js.map