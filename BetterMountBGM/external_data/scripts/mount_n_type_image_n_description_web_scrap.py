#!/usr/bin/env python3
"""
FFXIV Complete Mount Scraper
- Scrapes all mounts from FFXIV Wiki
- Optionally downloads mount type icons
- Fetches individual mount pages to extract in-game descriptions

Requirements:
    pip install beautifulsoup4 requests
"""

import json
import re
import os
import time
import requests
from bs4 import BeautifulSoup
from urllib.parse import urljoin

# ==========================
# CONFIG
# ==========================
DOWNLOAD_ICONS = False      # Toggle icon downloads
REQUEST_DELAY = 0.2         # Delay between individual mount page requests (seconds)

MOUNTS_URL = "https://ffxiv.consolegameswiki.com/wiki/Mounts"
BASE_URL = "https://ffxiv.consolegameswiki.com"

# Cache to avoid refetching descriptions
DESCRIPTION_CACHE = {}

# ==========================
# HELPERS
# ==========================

def clean_text(text):
    if not text:
        return ""
    text = ' '.join(text.split())
    text = re.sub(r'\[.*?\]', '', text)
    return text.strip()


def download_icon(img_url, save_folder, icon_name):
    try:
        os.makedirs(save_folder, exist_ok=True)

        full_url = re.sub(
            r'/thumb(/.*?)/\d+px-.*?$',
            r'\1',
            img_url
        )

        response = requests.get(full_url, timeout=10)
        response.raise_for_status()

        filepath = os.path.join(save_folder, icon_name)
        with open(filepath, 'wb') as f:
            f.write(response.content)

        return True
    except Exception as e:
        print(f"  ⚠️  Failed to download {icon_name}: {e}")
        return False


def fetch_mount_description(mount_url):
    """Fetch and cache in-game description from individual mount page"""
    if mount_url in DESCRIPTION_CACHE:
        return DESCRIPTION_CACHE[mount_url]

    try:
        response = requests.get(mount_url, timeout=15)
        response.raise_for_status()
    except Exception as e:
        print(f"  ⚠️  Failed to fetch description: {mount_url} ({e})")
        DESCRIPTION_CACHE[mount_url] = ""
        return ""

    soup = BeautifulSoup(response.content, 'html.parser')

    blockquote = soup.find('blockquote')
    if not blockquote:
        DESCRIPTION_CACHE[mount_url] = ""
        return ""

    text = blockquote.get_text(separator="\n")
    text = re.sub(r'—\s*In-game description.*$', '', text, flags=re.I)

    description = clean_text(text)
    DESCRIPTION_CACHE[mount_url] = description

    time.sleep(REQUEST_DELAY)
    return description


# ==========================
# CORE EXTRACTION
# ==========================

def extract_mount_data(row, type_icons):
    try:
        cols = row.find_all('td')

        # 🔥 CORREÇÃO CRÍTICA AQUI
        if len(cols) < 10:
            return None

        # Name + URL
        name_link = cols[1].find('a')
        if not name_link:
            return None

        name = clean_text(name_link.get_text())
        mount_url = urljoin(BASE_URL, name_link['href'])

        print(f"Processing: {name}")

        # Type
        mount_type = clean_text(cols[3].get_text())

        # Type icon
        type_img = cols[2].find('img')
        if type_img and type_img.get('src'):
            img_src = type_img['src']
            full_img_src = re.sub(
                r'/thumb(/[^/]+/[^/]+/[^/]+\.png)/\d+px-.*',
                r'\1',
                img_src
            ) if '/thumb/' in img_src else img_src

            full_img_url = urljoin(BASE_URL, full_img_src)
            icon_filename = full_img_src.split('/')[-1]

            if mount_type and mount_type not in type_icons:
                type_icons[mount_type] = {
                    "url": full_img_url,
                    "filename": icon_filename
                }

        # Acquisition
        acquired_by = clean_text(cols[4].get_text())

        # Obtainable
        obtainable_cell = cols[5]
        obtainable = (
            clean_text(obtainable_cell.get_text()) == '1' or
            'currently obtainable' in obtainable_cell.get('title', '').lower() or
            obtainable_cell.find('img', alt=re.compile(r'yes|check|true', re.I))
        )

        # Cash Shop
        cash_shop_cell = cols[6]
        cash_shop = (
            clean_text(cash_shop_cell.get_text()) == '1' or
            'online store' in cash_shop_cell.get('title', '').lower() or
            cash_shop_cell.find('img', alt=re.compile(r'yes|check|true', re.I))
        )

        # Market Board
        mb_cell = cols[7]
        market_board = (
            clean_text(mb_cell.get_text()) == '1' or
            'market board' in mb_cell.get('title', '').lower() or
            mb_cell.find('img', alt=re.compile(r'yes|check|true', re.I))
        )

        # Seats
        try:
            seats = int(clean_text(cols[8].get_text()))
        except:
            seats = 1

        # Patch
        patch = clean_text(cols[9].get_text())

        # Description (individual page)
        description = fetch_mount_description(mount_url)

        return {
            "name": name,
            "type": mount_type,
            "acquired_by": acquired_by,
            "patch": patch,
            "seats": seats,
            "obtainable": bool(obtainable),
            "cash_shop": bool(cash_shop),
            "market_board": bool(market_board),
            "description": description,
            "wiki_url": mount_url
        }

    except Exception as e:
        print(f"Error processing row: {e}")
        return None


# ==========================
# SCRAPER
# ==========================

def scrape_mounts():
    print(f"Fetching mounts page: {MOUNTS_URL}")

    response = requests.get(MOUNTS_URL, timeout=30)
    response.raise_for_status()

    soup = BeautifulSoup(response.content, 'html.parser')
    tables = soup.find_all('table')

    mount_table = None
    for table in tables:
        headers = table.find_all('th')
        header_text = ' '.join(h.get_text() for h in headers)
        if 'Name' in header_text and 'Seats' in header_text:
            mount_table = table
            break

    if not mount_table:
        raise RuntimeError("Mount table not found")

    rows = mount_table.find_all('tr')[1:]

    mounts = {}
    type_icons = {}
    mount_id = 1

    print(f"Processing {len(rows)} mounts...\n")

    for row in rows:
        mount = extract_mount_data(row, type_icons)
        if mount:
            mounts[str(mount_id)] = mount
            mount_id += 1

    return mounts, type_icons


# ==========================
# MAIN
# ==========================

def main():
    print(">>> SCRIPT STARTED <<<\n")

    mounts, type_icons = scrape_mounts()

    if DOWNLOAD_ICONS and type_icons:
        print(f"\nDownloading {len(type_icons)} type icons...")
        for type_name, icon in type_icons.items():
            download_icon(icon['url'], "type_icons", icon['filename'])

    output = {
        "version": "1.1.1",
        "last_updated": "2026-01-03",
        "source": MOUNTS_URL,
        "total_mounts": len(mounts),
        "mounts": mounts
    }

    with open("mount_sources_complete.json", "w", encoding="utf-8") as f:
        json.dump(output, f, indent=2, ensure_ascii=False)

    print("\nDONE!")
    print("Saved: mount_sources_complete.json")
    print(f"Total mounts: {len(mounts)}")


if __name__ == "__main__":
    main()
