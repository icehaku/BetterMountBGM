#!/usr/bin/env python3
"""
FFXIV Complete Mount Scraper
Extracts ALL mounts from the FFXIV Wiki automatically

Requirements:
    pip install beautifulsoup4 requests

Usage:
    python3 scrape_all_mounts_wiki.py
    
Output:
    mount_sources_complete.json - Complete database with all mounts
"""

import json
import re
import requests
from bs4 import BeautifulSoup

def clean_text(text):
    """Clean and normalize text from wiki"""
    if not text:
        return ""
    # Remove extra whitespace
    text = ' '.join(text.split())
    # Remove wiki markup
    text = re.sub(r'\[.*?\]', '', text)
    return text.strip()

def extract_mount_data(row):
    """Extract mount data from a table row"""
    try:
        cols = row.find_all('td')
        if len(cols) < 9:
            return None
        
        # Column indices:
        # 0: Icon (empty)
        # 1: Name (with link)
        # 2: Icon (duplicate)
        # 3: Acquisition Type
        # 4: Acquired By
        # 5: Obtainable?
        # 6: Cash Shop?
        # 7: Market Board?
        # 8: Seats
        # 9: Patch
        
        # Extract name
        name_link = cols[1].find('a')
        if name_link:
            name = clean_text(name_link.get_text())
        else:
            name = clean_text(cols[1].get_text())
        
        if not name:
            return None
        
        # Extract type
        mount_type = clean_text(cols[3].get_text())
        
        # Extract acquisition method
        acquired_by = clean_text(cols[4].get_text())
        
        # Extract obtainable status (check the image/icon or text content)
        obtainable_cell = cols[5]
        # Wiki uses "1" for true/obtainable and "0" for false/not obtainable
        obtainable_text = clean_text(obtainable_cell.get_text())
        obtainable_title = obtainable_cell.get('title', '')
        # Check multiple indicators
        obtainable = (
            obtainable_text == '1' or 
            'currently obtainable' in obtainable_title.lower() or
            obtainable_cell.find('img', alt=re.compile(r'yes|check|true', re.I)) is not None
        )
        
        # Extract cash shop status
        cash_shop_cell = cols[6]
        cash_shop_text = clean_text(cash_shop_cell.get_text())
        cash_shop_title = cash_shop_cell.get('title', '')
        # Check multiple indicators
        cash_shop = (
            cash_shop_text == '1' or
            'for sale in the online store' in cash_shop_title.lower() or
            cash_shop_cell.find('img', alt=re.compile(r'yes|check|true', re.I)) is not None
        )
        
        # Extract market board status
        mb_cell = cols[7]
        mb_text = clean_text(mb_cell.get_text())
        mb_title = mb_cell.get('title', '')
        # Check multiple indicators
        market_board = (
            mb_text == '1' or
            'may be purchased on the market board' in mb_title.lower() or
            mb_cell.find('img', alt=re.compile(r'yes|check|true', re.I)) is not None
        )
        
        # Extract seats
        try:
            seats = int(clean_text(cols[8].get_text()))
        except:
            seats = 1
        
        # Extract patch
        patch = clean_text(cols[9].get_text())
        
        return {
            "name": name,
            "type": mount_type,
            "acquired_by": acquired_by,
            "patch": patch,
            "seats": seats,
            "obtainable": obtainable,
            "cash_shop": cash_shop,
            "market_board": market_board
        }
    
    except Exception as e:
        print(f"Error processing row: {e}")
        return None

def scrape_mounts_from_url():
    """Scrape mounts directly from the wiki URL"""
    url = "https://ffxiv.consolegameswiki.com/wiki/Mounts"
    
    print(f"📥 Fetching data from {url}...")
    
    try:
        response = requests.get(url, timeout=30)
        response.raise_for_status()
        print("✅ Page fetched successfully")
    except Exception as e:
        print(f"❌ Error fetching page: {e}")
        return None
    
    soup = BeautifulSoup(response.content, 'html.parser')
    
    # Find the main mounts table
    # The table has headers: Icon | Name | Icon | Type | Acquired By | Obtainable | Cash | MB | Seats | Patch
    print("🔍 Searching for mounts table...")
    
    # Find table by looking for the header row
    tables = soup.find_all('table')
    mount_table = None
    
    for table in tables:
        headers = table.find_all('th')
        header_text = ' '.join([h.get_text() for h in headers])
        if 'Name' in header_text and 'Acquired By' in header_text and 'Seats' in header_text:
            mount_table = table
            break
    
    if not mount_table:
        print("❌ Could not find mounts table")
        return None
    
    print("✅ Found mounts table")
    
    # Extract all mount rows
    mounts = {}
    rows = mount_table.find_all('tr')[1:]  # Skip header row
    
    mount_id = 1
    successful = 0
    failed = 0
    
    print(f"📊 Processing {len(rows)} rows...")
    
    for row in rows:
        mount_data = extract_mount_data(row)
        if mount_data:
            mounts[str(mount_id)] = mount_data
            mount_id += 1
            successful += 1
        else:
            failed += 1
    
    print(f"✅ Successfully extracted {successful} mounts")
    if failed > 0:
        print(f"⚠️  Failed to extract {failed} rows")
    
    return mounts

def scrape_mounts_from_file(html_file):
    """Scrape mounts from a local HTML file"""
    print(f"📥 Reading from {html_file}...")
    
    try:
        with open(html_file, 'r', encoding='utf-8') as f:
            html_content = f.read()
        print("✅ File read successfully")
    except Exception as e:
        print(f"❌ Error reading file: {e}")
        return None
    
    soup = BeautifulSoup(html_content, 'html.parser')
    
    print("🔍 Searching for mounts table...")
    
    # Find table by looking for the header row
    tables = soup.find_all('table')
    mount_table = None
    
    for table in tables:
        headers = table.find_all('th')
        header_text = ' '.join([h.get_text() for h in headers])
        if 'Name' in header_text and 'Acquired By' in header_text and 'Seats' in header_text:
            mount_table = table
            break
    
    if not mount_table:
        print("❌ Could not find mounts table")
        return None
    
    print("✅ Found mounts table")
    
    # Extract all mount rows
    mounts = {}
    rows = mount_table.find_all('tr')[1:]  # Skip header row
    
    mount_id = 1
    successful = 0
    failed = 0
    
    print(f"📊 Processing {len(rows)} rows...")
    
    for row in rows:
        mount_data = extract_mount_data(row)
        if mount_data:
            mounts[str(mount_id)] = mount_data
            mount_id += 1
            successful += 1
        else:
            failed += 1
    
    print(f"✅ Successfully extracted {successful} mounts")
    if failed > 0:
        print(f"⚠️  Failed to extract {failed} rows")
    
    return mounts

def main():
    """Main function"""
    print("=" * 60)
    print("FFXIV Complete Mount Scraper")
    print("=" * 60)
    print()
    
    # Try to scrape from URL first
    mounts = scrape_mounts_from_url()
    
    # If URL fails, try local file
    if not mounts:
        print("\n⚠️  URL scraping failed, trying local file...")
        print("Please save the wiki page as 'mounts_wiki.html' in the same directory")
        mounts = scrape_mounts_from_file('mounts_wiki.html')
    
    if not mounts:
        print("\n❌ Failed to extract mounts. Exiting.")
        return
    
    # Create output JSON
    output = {
        "version": "1.0.0",
        "last_updated": "2026-01-02",
        "source": "https://ffxiv.consolegameswiki.com/wiki/Mounts",
        "total_mounts": len(mounts),
        "note": "Complete mount database for FFXIV. IDs are sequential placeholders - use mount NAME for matching with in-game data.",
        "mounts": mounts
    }
    
    # Save to file
    output_file = 'mount_sources_complete.json'
    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(output, f, indent=2, ensure_ascii=False)
    
    print(f"\n📁 Saved to: {output_file}")
    print(f"📊 Total mounts: {len(mounts)}")
    
    # Statistics
    obtainable = sum(1 for m in mounts.values() if m['obtainable'])
    unobtainable = len(mounts) - obtainable
    cash_shop = sum(1 for m in mounts.values() if m['cash_shop'])
    market_board = sum(1 for m in mounts.values() if m['market_board'])
    
    print(f"\n📈 Statistics:")
    print(f"   Obtainable: {obtainable}")
    print(f"   Unobtainable: {unobtainable}")
    print(f"   Cash Shop: {cash_shop}")
    print(f"   Market Board: {market_board}")
    
    # Show some examples
    print(f"\n📝 Sample entries:")
    for i, (id, mount) in enumerate(list(mounts.items())[:5], 1):
        print(f"   {i}. {mount['name']} - {mount['type']}")
    
    print("\n✅ Done!")

if __name__ == "__main__":
    main()