import feedparser

# Example RSS feed URL (replace with actual URL if needed)
rss_url = "https://moxie.foxnews.com/google-publisher/latest.xml"

# Parse the feed
feed = feedparser.parse(rss_url)

# List of titles (or other fields) to exclude (optional)
excluded_titles = [
    # Add exact titles or patterns you want to exclude
]

# Print feed metadata
print("Feed Title:", feed.feed.title)
print("Feed Link:", feed.feed.link)
print("Feed Description:", feed.feed.description)
print("Feed Language:", feed.feed.language)
print("Feed Copyright:", feed.get("copyright", "N/A"))
print("Feed Publication Date:", feed.feed.get("published", "N/A"))
print("\n" + "="*80 + "\n")

# Print entries, skipping excluded ones
for entry in feed.entries:
    title = entry.title
    link = entry.link
    description = entry.description
    pub_date = entry.get("published", "N/A")
    categories = entry.get("tags", [])
    # Get full HTML content via content:encoded namespace
    content_encoded = entry.get("content:encoded", "N/A")

    # Example: Skip if title is in exclusion list
    if title in excluded_titles:
        continue

    # Print entry details
    print("Title:", title)
    print("Link:", link)
    print("Description:", description)
    print("Published:", pub_date)
    print("Categories:", [tag.term for tag in categories] if hasattr(categories[0], 'term') else categories)
    # Media content (e.g., images)
    #media = [m.url for m in entry.get("media_content", [])]
    media = [m['url'] for m in entry.get("media_content", []) if 'url' in m]

    if media:
        print("Media:", media)
    # Full HTML content
    print("Content (HTML):", content_encoded[:200] + "..." if content_encoded != "N/A" else "N/A")
    print("-"*80)

