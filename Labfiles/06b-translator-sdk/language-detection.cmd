set region="westeurope"
set key=""

curl -X POST "https://api.cognitive.microsofttranslator.com/detect?api-version=3.0" -H "Ocp-Apim-Subscription-Key: %key%" -H "Ocp-Apim-Subscription-Region: %region%" -H "Content-Type: application/json; charset=UTF-8" -d "[{ 'Text' : 'Guten morgen' }]"