#!/usr/bin/env bash
# Point ownly.win at GitHub Pages (OwnlyTeam/Ownly) via the Cloudflare API.
#
# 1. Make a token:  Cloudflare -> My Profile -> API Tokens -> Create Token
#    -> use the "Edit zone DNS" template
#    -> Zone Resources: Include -> Specific zone -> ownly.win
#    -> Continue -> Create Token -> copy it.
# 2. Run:  bash setup-dns.sh
#    (it will prompt for the token; nothing is stored)
set -euo pipefail

ZONE="ownly.win"
IPS=(185.199.108.153 185.199.109.153 185.199.110.153 185.199.111.153)
API="https://api.cloudflare.com/client/v4"

read -rsp "Paste Cloudflare API token: " T; echo
H=(-H "Authorization: Bearer $T" -H "Content-Type: application/json")

zid=$(curl -s "${H[@]}" "$API/zones?name=$ZONE" | grep -o '"id":"[a-f0-9]\{32\}"' | head -1 | cut -d'"' -f4)
[ -n "$zid" ] || { echo "Could not find zone $ZONE with that token."; exit 1; }
echo "zone: $zid"

# delete existing root + www A/AAAA/CNAME records
for n in "$ZONE" "www.$ZONE"; do
  for t in A AAAA CNAME; do
    for rid in $(curl -s "${H[@]}" "$API/zones/$zid/dns_records?name=$n&type=$t" \
                 | grep -o '"id":"[a-f0-9]\{32\}"' | cut -d'"' -f4); do
      curl -s -X DELETE "${H[@]}" "$API/zones/$zid/dns_records/$rid" >/dev/null
      echo "deleted old $t $n"
    done
  done
done

for ip in "${IPS[@]}"; do
  curl -s -X POST "${H[@]}" "$API/zones/$zid/dns_records" \
    --data "{\"type\":\"A\",\"name\":\"$ZONE\",\"content\":\"$ip\",\"proxied\":false,\"ttl\":1}" >/dev/null
  echo "added  A     @    -> $ip"
done
curl -s -X POST "${H[@]}" "$API/zones/$zid/dns_records" \
  --data "{\"type\":\"CNAME\",\"name\":\"www\",\"content\":\"ownlyteam.github.io\",\"proxied\":false,\"ttl\":1}" >/dev/null
echo "added  CNAME www  -> ownlyteam.github.io"

echo
echo "Records now:"
curl -s "${H[@]}" "$API/zones/$zid/dns_records?per_page=100" \
  | grep -o '"type":"[A-Z]*","name":"[^"]*","content":"[^"]*"' \
  | sed 's/"type":"//;s/","name":"/  /;s/","content":"/  ->  /'
echo
echo 'Done. Tell Claude "dns done".'
