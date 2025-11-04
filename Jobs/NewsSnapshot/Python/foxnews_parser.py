import json
import re
import sys
from datetime import datetime
from bs4 import BeautifulSoup

html = sys.stdin.read()            # Read HTML from stdin

soup = BeautifulSoup(html, 'html.parser')

# Match article links (e.g., /2025/09/26/...)
pattern = re.compile(r'^/\d{4}/\d{2}/\d{2}')
date_pattern = re.compile(r'^/(\d{4})/(\d{2})/(\d{2})')

results = []

# Capture links matching /yyyy/mm/dd
#links = [a['href'] for a in soup.find_all('a', href=True) if pattern.match(a['href'])]
#links = [a['href'] for a in soup.find_all('a', href=True)]
#print(links)
#unique_links = list(dict.fromkeys(links))  # Remove duplicates, keep order

excluded_contains = ["shop.foxnews.com", "radio.foxnews.com", "noticias.foxnews.com", "foxadvertising.com",
                     "fox.com", "foxcareers.com", "nation.foxnews.com", "press.foxnews.com", "open.spotify.com"]

excluded_prefixes = ["#", "https://outkick.com", "https://www.outkick.com", "http://bit.ly", 
                     "https://www.facebook.com", "https://flipboard.com", "https://www.instagram.com", "https://www.linkedin.com", "https://twitter.com",
                     "https://www.youtube.com", "https://www.iheart.com",
                     "https://www.foxnews.com/books", "https://www.foxnews.com/video",
                     "https://www.foxnews.com/category", "https://www.foxnews.com/newsletters",
                     "https://www.foxbusiness.com/quote", "https://www.foxbusiness.com/watchlist"]

excluded_exact_matches = ["https://www.foxnews.com/", "https://foxnews.com/", "https://www.foxnews.com", "https://foxnews.com",
                          "https://www.foxweather.com/", "https://www.foxweather.com",
                          "https://www.foxbusiness.com/", "https://foxbusiness.com/", "https://www.foxbusiness.com", "https://foxbusiness.com",
                          "https://www.foxbusiness.com/economy", "https://www.foxbusiness.com/markets",
                          "https://www.foxbusiness.com/personal-finance", "https://www.foxbusiness.com/lifestyle",
                          "https://www.foxbusiness.com/real-estate", "https://www.foxbusiness.com/technology"]


for a in soup.find_all('a', href=True):
    href = a['href']
    
    #if pattern.match(href):
        #m = date_pattern.match(href)
        #if not m:
        #    continue
        #y, mo, d = map(int, m.groups())
        #link_date = datetime(y, mo, d)

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
        #results.append({
        #    'date': link_date,
        #    'link': href,
        #    'headline': headline if headline else '(No headline)'
        #})

    if href.startswith(r"//"):
        continue

    if href in excluded_exact_matches:
        continue

    if any(x in href for x in excluded_contains):
        continue

    if href.startswith(tuple(excluded_prefixes)):
        continue

    

    #if (href == "https://www.foxnews.com/" or href == "https://www.foxbusiness.com/" or 
     #   href == "https://www.foxweather.com/" or href == "https://www.foxnews.com" or 
      #  href == "#"):
       # continue

    results.append({
        'link': href,
        'headline': headline if headline else '(No headline)'
    })

# Deduplicate by link and sort by date
unique = {}
for item in results:
    if item['link'] not in unique:
        unique[item['link']] = item

#final = sorted(unique.values(), key=lambda x: x[0], reverse=True)

#print(json.dumps(unique_links, indent=4))
#print(json.dumps(unique, indent=4))
print(json.dumps(list(unique.values()), indent=4))
