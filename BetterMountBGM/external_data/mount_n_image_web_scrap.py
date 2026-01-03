#!/usr/bin/env python3
"""
FFXIV Complete Mount Scraper
Extracts ALL mounts from the FFXIV Wiki automatically + Downloads type icons

Requirements:
    pip install beautifulsoup4 requests

Usage:
    python3 mount_web_scrap.py
    
Output:
    mount_sources_complete.json - Complete database with all mounts
    type_icons/ - Folder with all type icons downloaded
"""

import json
import re
import os
import requests
from bs4 import BeautifulSoup
from urllib.parse import urljoin

def clean_text(text):
    """Clean and normalize text from wiki"""
    if not text:
        return ""
    # Remove extra whitespace
    text = ' '.join(text.split())
    # Remove wiki markup
    text = re.sub(r'\[.*?\]', '', text)
    return text.strip()

def download_icon(img_url, save_folder, icon_name):
    """Download an icon from URL and save it"""
    try:
        # Create folder if it doesn't exist
        os.makedirs(save_folder, exist_ok=True)
        
        # Get full resolution image (remove thumb/sizing from URL)
        # Example: /thumb/c/c4/Gold_saucer_icon1.png/40px-Gold_saucer_icon1.png
        # Becomes: /c/c4/Gold_saucer_icon1.png
        full_url = re.sub(r'/thumb(/.*?)/\d+px-.*?$', r'\1', img_url)
        
        response = requests.get(full_url, timeout=10)
        response.raise_for_status()
        
        # Save icon
        filepath = os.path.join(save_folder, icon_name)
        with open(filepath, 'wb') as f:
            f.write(response.content)
        
        return True
    except Exception as e:
        print(f"  ⚠️  Failed to download {icon_name}: {e}")
        return False

def extract_mount_data(row, type_icons, base_url):
    """Extract mount data from a table row and collect type icons"""
    try:
        cols = row.find_all('td')
        if len(cols) < 9:
            return None
        
        # Column indices:
        # 0: Icon (empty)
        # 1: Name (with link)
        # 2: Icon (duplicate)
        # 3: Acquisition Type (with icon)
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
        
        # Extract type and its icon
        type_cell = cols[3]
        mount_type = clean_text(type_cell.get_text())

        # DEBUG: Ver se tem imagem
        if cols[3].find('img'):
            print(f"DEBUG: {mount_type} has image: {cols[3].find('img').get('src', 'NO SRC')[:80]}")
        
        # Extract type text from column 3
        mount_type = clean_text(cols[3].get_text())

        # Extract type icon from column 2 (icon is separate from text)
        type_img = cols[2].find('img')
        
        if type_img and type_img.get('src'):
            img_src = type_img.get('src')
            
            # Construir URL completa sem thumb
            if '/thumb/' in img_src:
                full_img_src = re.sub(r'/thumb(/[^/]+/[^/]+/[^/]+\.png)/\d+px-.*', r'\1', img_src)
                icon_filename = full_img_src.split('/')[-1]
            else:
                full_img_src = img_src
                icon_filename = img_src.split('/')[-1]
            
            full_img_url = urljoin(base_url, full_img_src)
            
            if mount_type and mount_type not in type_icons:
                type_icons[mount_type] = {
                    'url': full_img_url,
                    'filename': icon_filename
                }
                
        # Extract acquisition method
        acquired_by = clean_text(cols[4].get_text())
        
        # Extract obtainable status
        obtainable_cell = cols[5]
        obtainable_text = clean_text(obtainable_cell.get_text())
        obtainable_title = obtainable_cell.get('title', '')
        obtainable = (
            obtainable_text == '1' or 
            'currently obtainable' in obtainable_title.lower() or
            obtainable_cell.find('img', alt=re.compile(r'yes|check|true', re.I)) is not None
        )
        
        # Extract cash shop status
        cash_shop_cell = cols[6]
        cash_shop_text = clean_text(cash_shop_cell.get_text())
        cash_shop_title = cash_shop_cell.get('title', '')
        cash_shop = (
            cash_shop_text == '1' or
            'for sale in the online store' in cash_shop_title.lower() or
            cash_shop_cell.find('img', alt=re.compile(r'yes|check|true', re.I)) is not None
        )
        
        # Extract market board status
        mb_cell = cols[7]
        mb_text = clean_text(mb_cell.get_text())
        mb_title = mb_cell.get('title', '')
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
    base_url = "https://ffxiv.consolegameswiki.com"
    
    print(f"📥 Fetching data from {url}...")
    
    try:
        response = requests.get(url, timeout=30)
        response.raise_for_status()
        print("✅ Page fetched successfully")
    except Exception as e:
        print(f"❌ Error fetching page: {e}")
        return None, None
    
    soup = BeautifulSoup(response.content, 'html.parser')
    
    # Find the main mounts table
    print("🔍 Searching for mounts table...")
    
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
        return None, None
    
    print("✅ Found mounts table")
    
    # Extract all mount rows and collect type icons
    mounts = {}
    type_icons = {}  # Dictionary to store unique type icons
    rows = mount_table.find_all('tr')[1:]  # Skip header row
    
    mount_id = 1
    successful = 0
    failed = 0
    
    print(f"📊 Processing {len(rows)} rows...")
    
    for row in rows:
        mount_data = extract_mount_data(row, type_icons, base_url)
        if mount_data:
            mounts[str(mount_id)] = mount_data
            mount_id += 1
            successful += 1
        else:
            failed += 1
    
    print(f"✅ Successfully extracted {successful} mounts")
    if failed > 0:
        print(f"⚠️  Failed to extract {failed} rows")
    
    return mounts, type_icons

def main():
    """Main function"""
    print("=" * 60)
    print("FFXIV Complete Mount Scraper + Icon Downloader")
    print("=" * 60)
    print()
    
    # Scrape mounts and collect icon info
    mounts, type_icons = scrape_mounts_from_url()
    
    if not mounts:
        print("\n❌ Failed to extract mounts. Exiting.")
        return
    
    # Download type icons
    if type_icons:
        print(f"\n🖼️  Downloading {len(type_icons)} unique type icons...")
        icons_folder = 'type_icons'
        downloaded = 0
        
        for type_name, icon_info in type_icons.items():
            print(f"  📥 {type_name}: {icon_info['filename']}")
            if download_icon(icon_info['url'], icons_folder, icon_info['filename']):
                downloaded += 1
        
        print(f"✅ Downloaded {downloaded}/{len(type_icons)} icons to '{icons_folder}/'")
    
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
    
    # Show unique types
    unique_types = set(m['type'] for m in mounts.values())
    print(f"\n📋 Unique Types ({len(unique_types)}):")
    for type_name in sorted(unique_types):
        icon_file = type_icons.get(type_name, {}).get('filename', 'No icon')
        print(f"   - {type_name}: {icon_file}")
    
    print("\n✅ Done!")

if __name__ == "__main__":
    main()