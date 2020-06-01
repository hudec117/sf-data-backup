# References:
# https://help.salesforce.com/articleView?id=remoteaccess_oauth_jwt_flow.htm&type=5
# https://stackoverflow.com/questions/45298354/how-to-sign-a-token-with-rsa-sha-256-in-python

import base64
from datetime import datetime

def main():
    clientId = ''
    audience = 'https://login.salesforce.com'
    user = ''

    header = '{"alg":"RS256"}'
    encodedHeader = encode(header)

    expiry = getExpiryTimestamp() + 300
    claimsSet = f'''{{"iss":"{clientId}","sub":"{user}","aud":"{audience}","exp":"{expiry}"}}'''

    encodedClaimsSet = encode(claimsSet)

    headerAndClaimsSet = f'{encodedHeader}.{encodedClaimsSet}'

def encode(value):
    valueBytes = bytes(value, 'utf-8')
    return base64.b64encode(valueBytes)

def getExpiryTimestamp():
    now = datetime.utcnow()
    return int((now - datetime(1970, 1, 1)).total_seconds())

if __name__ == '__main__':
    main()