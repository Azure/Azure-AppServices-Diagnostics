"use strict";
var __assign = (this && this.__assign) || function () {
    __assign = Object.assign || function(t) {
        for (var s, i = 1, n = arguments.length; i < n; i++) {
            s = arguments[i];
            for (var p in s) if (Object.prototype.hasOwnProperty.call(s, p))
                t[p] = s[p];
        }
        return t;
    };
    return __assign.apply(this, arguments);
};
Object.defineProperty(exports, "__esModule", { value: true });
/* --------------------------------------------------------------------------------------------
 * Copyright (c) 2018 TypeFox GmbH (http://www.typefox.io). All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */
var fs = require("fs");
var request_light_1 = require("request-light");
var vscode_uri_1 = require("vscode-uri");
var vscode_languageserver_1 = require("vscode-languageserver");
var vscode_json_languageservice_1 = require("vscode-json-languageservice");
function start(reader, writer) {
    var connection = vscode_languageserver_1.createConnection(reader, writer);
    var server = new JsonServer(connection);
    server.start();
    return server;
}
exports.start = start;
var JsonServer = /** @class */ (function () {
    function JsonServer(connection) {
        var _this = this;
        this.connection = connection;
        this.documents = new vscode_languageserver_1.TextDocuments();
        this.jsonService = vscode_json_languageservice_1.getLanguageService({
            schemaRequestService: this.resovleSchema.bind(this)
        });
        this.pendingValidationRequests = new Map();
        this.documents.listen(this.connection);
        this.documents.onDidChangeContent(function (change) {
            return _this.validate(change.document);
        });
        this.documents.onDidClose(function (event) {
            _this.cleanPendingValidation(event.document);
            _this.cleanDiagnostics(event.document);
        });
        this.connection.onInitialize(function (params) {
            if (params.rootPath) {
                _this.workspaceRoot = vscode_uri_1.default.file(params.rootPath);
            }
            else if (params.rootUri) {
                _this.workspaceRoot = vscode_uri_1.default.parse(params.rootUri);
            }
            _this.connection.console.log("The server is initialized.");
            return {
                capabilities: {
                    textDocumentSync: _this.documents.syncKind,
                    codeActionProvider: true,
                    completionProvider: {
                        resolveProvider: true,
                        triggerCharacters: ['"', ':']
                    },
                    hoverProvider: true,
                    documentSymbolProvider: true,
                    documentRangeFormattingProvider: true,
                    executeCommandProvider: {
                        commands: ['json.documentUpper']
                    },
                    colorProvider: true,
                    foldingRangeProvider: true
                }
            };
        });
        this.connection.onCodeAction(function (params) {
            return _this.codeAction(params);
        });
        this.connection.onCompletion(function (params) {
            return _this.completion(params);
        });
        this.connection.onCompletionResolve(function (item) {
            return _this.resolveCompletion(item);
        });
        this.connection.onExecuteCommand(function (params) {
            return _this.executeCommand(params);
        });
        this.connection.onHover(function (params) {
            return _this.hover(params);
        });
        this.connection.onDocumentSymbol(function (params) {
            return _this.findDocumentSymbols(params);
        });
        this.connection.onDocumentRangeFormatting(function (params) {
            return _this.format(params);
        });
        this.connection.onDocumentColor(function (params) {
            return _this.findDocumentColors(params);
        });
        this.connection.onColorPresentation(function (params) {
            return _this.getColorPresentations(params);
        });
        this.connection.onFoldingRanges(function (params) {
            return _this.getFoldingRanges(params);
        });
    }
    JsonServer.prototype.start = function () {
        this.connection.listen();
    };
    JsonServer.prototype.getFoldingRanges = function (params) {
        var document = this.documents.get(params.textDocument.uri);
        if (!document) {
            return [];
        }
        return this.jsonService.getFoldingRanges(document);
    };
    JsonServer.prototype.findDocumentColors = function (params) {
        var document = this.documents.get(params.textDocument.uri);
        if (!document) {
            return Promise.resolve([]);
        }
        var jsonDocument = this.getJSONDocument(document);
        return this.jsonService.findDocumentColors(document, jsonDocument);
    };
    JsonServer.prototype.getColorPresentations = function (params) {
        var document = this.documents.get(params.textDocument.uri);
        if (!document) {
            return [];
        }
        var jsonDocument = this.getJSONDocument(document);
        return this.jsonService.getColorPresentations(document, jsonDocument, params.color, params.range);
    };
    JsonServer.prototype.codeAction = function (params) {
        var document = this.documents.get(params.textDocument.uri);
        if (!document) {
            return [];
        }
        return [{
                title: "Upper Case Document",
                command: "json.documentUpper",
                // Send a VersionedTextDocumentIdentifier
                arguments: [__assign({}, params.textDocument, { version: document.version })]
            }];
    };
    JsonServer.prototype.format = function (params) {
        var document = this.documents.get(params.textDocument.uri);
        return document ? this.jsonService.format(document, params.range, params.options) : [];
    };
    JsonServer.prototype.findDocumentSymbols = function (params) {
        var document = this.documents.get(params.textDocument.uri);
        if (!document) {
            return [];
        }
        var jsonDocument = this.getJSONDocument(document);
        return this.jsonService.findDocumentSymbols(document, jsonDocument);
    };
    JsonServer.prototype.executeCommand = function (params) {
        if (params.command === "json.documentUpper" && params.arguments) {
            var versionedTextDocumentIdentifier = params.arguments[0];
            var document_1 = this.documents.get(versionedTextDocumentIdentifier.uri);
            if (document_1) {
                this.connection.workspace.applyEdit({
                    documentChanges: [{
                            textDocument: versionedTextDocumentIdentifier,
                            edits: [{
                                    range: {
                                        start: { line: 0, character: 0 },
                                        end: { line: Number.MAX_SAFE_INTEGER, character: Number.MAX_SAFE_INTEGER }
                                    },
                                    newText: document_1.getText().toUpperCase()
                                }]
                        }]
                });
            }
        }
    };
    JsonServer.prototype.hover = function (params) {
        var document = this.documents.get(params.textDocument.uri);
        if (!document) {
            return Promise.resolve(null);
        }
        var jsonDocument = this.getJSONDocument(document);
        return this.jsonService.doHover(document, params.position, jsonDocument);
    };
    JsonServer.prototype.resovleSchema = function (url) {
        var uri = vscode_uri_1.default.parse(url);
        if (uri.scheme === 'file') {
            return new Promise(function (resolve, reject) {
                fs.readFile(uri.fsPath, 'UTF-8', function (err, result) {
                    err ? reject('') : resolve(result.toString());
                });
            });
        }
        return request_light_1.xhr({ url: url, followRedirects: 5 }).then(function (response) {
            return response.responseText;
        }, function (error) {
            return Promise.reject(error.responseText || request_light_1.getErrorStatusDescription(error.status) || error.toString());
        });
    };
    JsonServer.prototype.resolveCompletion = function (item) {
        return this.jsonService.doResolve(item);
    };
    JsonServer.prototype.completion = function (params) {
        var document = this.documents.get(params.textDocument.uri);
        if (!document) {
            return Promise.resolve(null);
        }
        var jsonDocument = this.getJSONDocument(document);
        return this.jsonService.doComplete(document, params.position, jsonDocument);
    };
    JsonServer.prototype.validate = function (document) {
        var _this = this;
        this.cleanPendingValidation(document);
        this.pendingValidationRequests.set(document.uri, setTimeout(function () {
            _this.pendingValidationRequests.delete(document.uri);
            _this.doValidate(document);
        }));
    };
    JsonServer.prototype.cleanPendingValidation = function (document) {
        var request = this.pendingValidationRequests.get(document.uri);
        if (request !== undefined) {
            clearTimeout(request);
            this.pendingValidationRequests.delete(document.uri);
        }
    };
    JsonServer.prototype.doValidate = function (document) {
        var _this = this;
        if (document.getText().length === 0) {
            this.cleanDiagnostics(document);
            return;
        }
        var jsonDocument = this.getJSONDocument(document);
        this.jsonService.doValidation(document, jsonDocument).then(function (diagnostics) {
            return _this.sendDiagnostics(document, diagnostics);
        });
    };
    JsonServer.prototype.cleanDiagnostics = function (document) {
        this.sendDiagnostics(document, []);
    };
    JsonServer.prototype.sendDiagnostics = function (document, diagnostics) {
        this.connection.sendDiagnostics({
            uri: document.uri, diagnostics: diagnostics
        });
    };
    JsonServer.prototype.getJSONDocument = function (document) {
        return this.jsonService.parseJSONDocument(document);
    };
    return JsonServer;
}());
exports.JsonServer = JsonServer;
//# sourceMappingURL=json-server.js.map