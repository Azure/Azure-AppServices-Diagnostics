from SearchModule import app
app.config.from_object("AppConfig.DevelopmentConfig")
if __name__ == "__main__":
    app.run("localhost", port=8010, threaded=True)