pipeline {
    agent any
    
    environment {
        DOTNET_VERSION = '8.0'
        NODE_VERSION = '20'
        ALLURE_VERSION = '2.24.1'
    }
    
    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }
        
        stage('Build Frontend') {
            agent {
                docker { 
                    image "node:${NODE_VERSION}-alpine"
                }
            }
            steps {
                dir('frontend') {
                    sh 'npm ci'
                    sh 'npm run build'
                }
            }
        }
        
        stage('Build Backend') {
            agent {
                docker { 
                    image "registry.access.redhat.com/ubi8/dotnet-80:8.0"
                }
            }
            steps {
                dir('backend') {
                    sh 'dotnet restore'
                    sh 'dotnet build --configuration Release --no-restore'
                }
            }
        }
        
        stage('Test Frontend') {
            agent {
                docker { 
                    image "node:${NODE_VERSION}-alpine"
                }
            }
            steps {
                dir('frontend') {
                    script {
                        try {
                            sh 'npm run test -- --run --reporter=allure-vitest/reporter'
                        } catch (Exception e) {
                            echo "Frontend tests failed: ${e.getMessage()}"
                            currentBuild.result = 'UNSTABLE'
                        }
                    }
                }
            }
            post {
                always {
                    dir('frontend') {
                        script {
                            def hasAllureResults = sh(
                                script: 'test -d allure-results && [ "$(find allure-results -type f 2>/dev/null | wc -l)" -gt 0 ]',
                                returnStatus: true
                            ) == 0
                            
                            if (hasAllureResults) {
                                echo "Stashing frontend/allure-results"
                                stash name: 'frontend-allure-results', includes: 'allure-results/**', allowEmpty: false
                            } else {
                                echo "No files in frontend/allure-results, skipping stash"
                            }
                        }
                    }
                }
            }
        }
        
        stage('Test Backend') {
            agent {
                docker {
                    image "registry.access.redhat.com/ubi8/dotnet-80:8.0"
                }
            }
            steps {
                dir('backend') {
                    script {
                        try {
                            // Устанавливаем переменную окружения для Allure
                            sh '''
                                export ALLURE_CONFIG=$(pwd)/Tests/bin/Release/net8.0/allureConfig.json
                                
                                // Запускаем тесты с Allure репортером
                                dotnet test \\
                                    --configuration Release \\
                                    --no-build \\
                                    --logger "trx;LogFileName=test-results.trx" \\
                                    --results-directory TestResults \\
                                    --verbosity normal \\
                                    --logger:"allure;ReportTargetPath=$(pwd)/allure-results"
                            '''
                        } catch (Exception e) {
                            echo "Backend tests failed: ${e.getMessage()}"
                            currentBuild.result = 'UNSTABLE'
                        }
                    }
                }
            }
            post {
                always {
                    dir('backend') {
                        script {
                            // Проверяем наличие результатов Allure
                            def hasAllureResults = sh(
                                script: 'test -d allure-results && [ "$(find allure-results -name "*.json" -type f | wc -l)" -gt 0 ]',
                                returnStatus: true
                            ) == 0
                            
                            if (hasAllureResults) {
                                echo "Found Allure results in backend, stashing..."
                                stash name: 'backend-allure-results', 
                                      includes: 'allure-results/**', 
                                      allowEmpty: false
                            } else {
                                echo "No Allure results found in backend"
                                
                                // Проверяем TRX результаты как fallback
                                def hasTrxResults = sh(
                                    script: 'test -d TestResults && [ "$(find TestResults -name "*.trx" -type f | wc -l)" -gt 0 ]',
                                    returnStatus: true
                                ) == 0
                                
                                if (hasTrxResults) {
                                    echo "Creating Allure results from TRX files"
                                    sh '''
                                        mkdir -p allure-results
                                        find TestResults -name "*.trx" -exec cp {} allure-results/ \\;
                                        # Создаем минимальный Allure результат для каждого теста
                                        cat > allure-results/executor.json << EOF
                                        {
                                            "name": "Jenkins",
                                            "type": "jenkins",
                                            "url": "${BUILD_URL}",
                                            "buildOrder": ${BUILD_NUMBER},
                                            "buildName": "${JOB_NAME}",
                                            "buildUrl": "${BUILD_URL}",
                                            "reportUrl": "",
                                            "reportName": "Allure Report"
                                        }
                                        EOF
                                    '''
                                    stash name: 'backend-allure-results', 
                                          includes: 'allure-results/**', 
                                          allowEmpty: false
                                }
                            }
                        }
                    }
                }
            }
        }
        
        stage('Prepare Allure Results') {
            steps {
                script {
                    sh 'rm -rf allure-results && mkdir -p allure-results'
                    echo "=== Restoring Allure results from previous stages ==="
                    
                    // Восстанавливаем результаты frontend тестов
                    try {
                        unstash 'frontend-allure-results'
                        sh '''
                            if [ -d "frontend/allure-results" ]; then
                                echo "Restored frontend/allure-results, copying..."
                                find frontend/allure-results -type f -name "*.json" -o -name "*.xml" -o -name "*.txt" | while read file; do
                                    cp -v "$file" allure-results/ 2>/dev/null || true
                                done
                                echo "Frontend results copied"
                            fi
                        '''
                    } catch (Exception e) {
                        echo "No frontend allure results to restore: ${e.getMessage()}"
                    }
                    
                    // Восстанавливаем результаты backend тестов
                    try {
                        unstash 'backend-allure-results'
                        sh '''
                            if [ -d "backend/allure-results" ]; then
                                echo "Restored backend/allure-results, copying..."
                                find backend/allure-results -type f -name "*.json" -o -name "*.xml" -o -name "*.trx" -o -name "*.txt" | while read file; do
                                    cp -v "$file" allure-results/ 2>/dev/null || true
                                done
                                echo "Backend results copied"
                            fi
                        '''
                    } catch (Exception e) {
                        echo "No backend allure results to restore: ${e.getMessage()}"
                    }
                    
                    // Создаем environment.properties для Allure
                    sh '''
                        cat > allure-results/environment.properties <<EOF
Branch=${BRANCH_NAME}
Build.Number=${BUILD_NUMBER}
Build.URL=${BUILD_URL}
Commit=${GIT_COMMIT}
Node.Version=${NODE_VERSION}
DotNet.Version=${DOTNET_VERSION}
Backend.Tests=true
Frontend.Tests=true
EOF
                    '''
                    
                    // Проверяем что есть результаты
                    sh '''
                        echo "=== Allure results summary ==="
                        find allure-results -type f | wc -l
                        echo "Files in allure-results:"
                        find allure-results -type f | head -20
                    '''
                }
            }
        }
        
        stage('Publish Allure Report') {
            steps {
                script {
                    def hasResults = sh(
                        script: 'test -d allure-results && [ "$(find allure-results -type f | wc -l)" -gt 0 ]',
                        returnStatus: true
                    ) == 0
                    
                    if (hasResults) {
                        echo "Publishing Allure report..."
                        allure([
                            includeProperties: false,
                            jdk: '',
                            properties: [],
                            reportBuildPolicy: 'ALWAYS',
                            results: [[path: 'allure-results']]
                        ])
                    } else {
                        echo "No Allure results found, skipping report generation"
                    }
                }
            }
        }
    }
    
    post {
        always {
            // Архивируем результаты
            script {
                def hasResults = sh(
                    script: 'test -d allure-results && [ "$(find allure-results -type f | wc -l)" -gt 0 ]',
                    returnStatus: true
                ) == 0
                
                if (hasResults) {
                    archiveArtifacts artifacts: 'allure-results/**', fingerprint: true, allowEmptyArchive: false
                }
            }
        }
        failure {
            echo 'Pipeline failed. Check the test results in Allure Report.'
        }
        success {
            echo 'Pipeline succeeded! All tests passed.'
        }
    }
}