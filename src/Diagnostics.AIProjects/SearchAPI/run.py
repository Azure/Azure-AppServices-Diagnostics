from SearchModule import app
app.config.from_object("AppConfig.DevelopmentConfig")
app.run("localhost", port=8010, threaded=True)