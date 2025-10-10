import json
import re
import requests
import sqlite3
import sys
from datetime import datetime
from bs4 import BeautifulSoup
import argparse

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--test-landing-page-file', type=str, help='Path to test landing page file')
    parser.add_argument('--db-path', type=str, required=True, help='Path to SQLite database file')
    parser.add_argument('--id', type=int, required=True, help='ID of the job run to update')
    args = parser.parse_args()

    if args.test_landing_page_file:
        with open(args.test_landing_page_file, 'r', encoding='utf-8') as f:
            html = f.read()
    else:
        url = "https://cnn.com"
        response = requests.get(url)
        html = response.text

    # Parse the HTML
    soup = BeautifulSoup(html, 'html.parser')

    # Remove all script tags and their contents
    for script in soup.find_all('script'):
        script.decompose()

    # Get the cleaned HTML back as a string, Remove extra whitespace, and save to DB
    cleaned_html = re.sub(r'\s+', ' ', str(soup))
    update_job_run(args.db_path, args.id, cleaned_html)

    # Match article links (e.g., /2025/09/26/...)
    pattern = re.compile(r'^/\d{4}/\d{2}/\d{2}')
    date_pattern = re.compile(r'^/(\d{4})/(\d{2})/(\d{2})')

    excluded_contains = ["/video/"]

    # Log only the actual href links found
    # hrefs = [a['href'] for a in soup.find_all('a', href=True)]
    # json_hrefs = json.dumps(hrefs)
    # update_job_run(args.db_path, args.id, json_hrefs)

    # Log the raw HTML for each <a> tag
    #hrefs_html = [str(a) for a in soup.find_all('a', href=True)]
    #json_hrefs = json.dumps(hrefs_html)
    #update_job_run(args.db_path, args.id, json_hrefs)

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

def update_job_run(db_path, job_id, raw_output):
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()
    cursor.execute(
        "UPDATE scrape_news_job_run SET raw_output = ? WHERE id = ?",
        (raw_output, job_id)
    )
    conn.commit()
    conn.close()

if __name__ == "__main__":
    main()
