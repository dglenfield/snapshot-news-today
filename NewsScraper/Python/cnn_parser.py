import json
import re
import requests
import sys
from datetime import datetime
from bs4 import BeautifulSoup

def main():

    url = "https://cnn.com"
    response = requests.get(url)
    html = response.text

    soup = BeautifulSoup(html, 'html.parser')

    # Match article links (e.g., /2025/09/26/...)
    pattern = re.compile(r'^/\d{4}/\d{2}/\d{2}')
    date_pattern = re.compile(r'^/(\d{4})/(\d{2})/(\d{2})')

    excluded_contains = ["/video/"]

    results = []
    for a in soup.find_all('a', href=True):
        href = a['href']

        if any(x in href for x in excluded_contains):
            continue

        if pattern.match(href):
            m = date_pattern.match(href)
            if not m:
                continue
            y, mo, d = map(int, m.groups())
            link_date = datetime(y, mo, d)

            # Look for headline inside <a> or parent
            headline_span = a.find('span', class_='container__headline-text')
            headline = headline_span.get_text(strip=True) if headline_span else None

            if headline is None:
                li_parent = a.find_parent('li')
                if li_parent:
                    fallback_span = li_parent.find('span', class_='container__headline-text')
                    if fallback_span:
                        headline = fallback_span.get_text(strip=True)

            # Add to results, even if no headline found
            results.append({
                'publishdate': link_date,
                'url': href,
                'headline': headline if headline else '(No headline)'
            })

    # Deduplicate by link and sort by date
    unique = {}
    for item in results:
        if item['url'] not in unique:
            unique[item['url']] = item

    sorted_values = sorted(unique.values(), key=lambda x: x['publishdate'], reverse=True)
    #sorted_values = unique.values()

    def serialize_datetime(obj):
        if isinstance(obj, datetime):
            return obj.isoformat()
        raise TypeError("Type not serializable")

    print(json.dumps(sorted_values, default=serialize_datetime, indent=2))
    #print(json.dumps(list(unique.values()), default=serialize_datetime, indent=2))

if __name__ == "__main__":
    main()
