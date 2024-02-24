set region="westeurope"
set key=""
set category=""

curl -X POST "https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&from=de&to=fr&to=en&category=%category%" -H "Ocp-Apim-Subscription-Key: %key%" -H "Ocp-Apim-Subscription-Region: %region%" -H "Content-Type: application/json; charset=UTF-8" -d "[{ 'Text' : 'Guten morgen' }]"