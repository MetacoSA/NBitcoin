pipeline {
    agent none
    stages {
        stage("build") {
            agent {
                docker { image 'mcr.microsoft.com/dotnet/sdk:6.0' }
            }

            environment {
                DOTNET_CLI_HOME = "/tmp/DOTNET_CLI_HOME"
                HOME = "/tmp"
            }
            steps {
                sh 'dotnet build'
            }
        }
    }
}
