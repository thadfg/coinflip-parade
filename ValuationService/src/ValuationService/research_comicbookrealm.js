const { chromium } = require('playwright');

/**
 * comicbookrealm.com Scraper
 * Extracts the "guide" price for a specific comic book.
 */
async function getComicBookRealmPrice(fullTitle) {
    const browser = await chromium.launch({ 
        headless: true,
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage']
    });
    
    const context = await browser.newContext({
        userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36',
        viewport: { width: 1280, height: 720 }
    });

    const page = await context.newPage();
    
    // Abort unnecessary requests to save memory and speed up
    await page.route('**/*.{png,jpg,jpeg,gif,webp,svg,css,woff,woff2}', route => route.abort());

    try {
        // 1. Search for the series. We extract the issue number from the title if possible.
        // Example: "Amazing Spider-Man #1" -> Query "Amazing Spider-Man", Issue "#1"
        let issueMatch = fullTitle.match(/#\s*(\d+(\.\d+)?)/);
        let issueNumber = issueMatch ? issueMatch[1] : null;
        let searchQuery = fullTitle;
        if (issueNumber) {
            searchQuery = fullTitle.split('#')[0].trim();
        }

        const encodedQuery = encodeURIComponent(searchQuery);
        // Using q= is the correct search parameter for ComicBookRealm
        let searchUrl = `https://comicbookrealm.com/search/all?q=${encodedQuery}`;
        
        console.error(`Searching at: ${searchUrl}`);
        await page.goto(searchUrl, { waitUntil: 'domcontentloaded', timeout: 30000 });
        await page.waitForTimeout(4000); // Wait for results table to populate

        // If we are redirected to a series page directly, skip search result parsing
        let currentUrl = page.url();
        let seriesLink = null;
        if (currentUrl.includes('/series/') && !currentUrl.includes('search-term') && !currentUrl.includes('q=')) {
            seriesLink = currentUrl;
            console.error(`Redirected directly to series: ${seriesLink}`);
        } else {
            // 2. Find the best series link. We look for links that match the search query.
            seriesLink = await page.evaluate((query) => {
                const links = Array.from(document.querySelectorAll('a'))
                    .filter(a => a.href.includes('/series/') && !a.href.includes('search-term') && !a.href.includes('q='));
                
                const cleanQuery = query.toLowerCase().replace(/[^\w\s]/g, '').trim();

                // Priority 1: Exact match (case-insensitive) on the series name part
                const exactMatch = links.find(l => {
                    const text = l.innerText.trim().toLowerCase();
                    const cleanText = text.replace(/[^\w\s]/g, '').trim();
                    return cleanText === cleanQuery || cleanText.startsWith(cleanQuery + " ");
                });
                if (exactMatch) return exactMatch.href;

                // Priority 2: Contains the query and looks like a main series link (not "Issue #")
                const containsMatch = links.find(l => {
                    const text = l.innerText.toLowerCase();
                    const cleanText = text.replace(/[^\w\s]/g, '').trim();
                    return cleanText.includes(cleanQuery) && !text.includes('issue #');
                });
                if (containsMatch) return containsMatch.href;

                // Priority 3: Fallback to the first series link
                if (links.length > 0) return links[0].href;

                return null;
            }, searchQuery);
        }

        if (!seriesLink) {
            console.error('No series found for query: ' + searchQuery);
            return null;
        }

        console.error(`Navigating to series: ${seriesLink}`);
        await page.goto(seriesLink, { waitUntil: 'domcontentloaded', timeout: 30000 });
        await page.waitForTimeout(2000);

        // 3. Find the price for the specific issue
        const price = await page.evaluate((issueNum) => {
            if (!issueNum) return null;
            
            const issuePattern = new RegExp(`#\\s*${issueNum}\\b`);
            const rows = Array.from(document.querySelectorAll('tr'));
            for (const row of rows) {
                const issueCell = row.querySelector('.issue');
                if (!issueCell) continue;

                const text = issueCell.innerText.replace(/\s+/g, ' ');
                if (issuePattern.test(text)) {
                    // Found the row. Now get the price from the .value cell.
                    const valueCell = row.querySelector('.value');
                    if (valueCell) {
                        const priceText = valueCell.innerText.replace(/[^\d.]/g, '');
                        if (priceText) return priceText;
                    }
                }
            }
            return null;
        }, issueNumber);

        if (price) {
            console.error(`Found price for #${issueNumber}: $${price}`);
            return price;
        }

        console.error(`Price not found for issue #${issueNumber}`);
        return null;

    } catch (error) {
        console.error("Error during research:", error);
        return null;
    } finally {
        await browser.close();
    }
}

const fullTitle = process.argv.slice(2).join(' ');
if (!fullTitle) {
    console.error("No title provided");
    process.exit(1);
}

getComicBookRealmPrice(fullTitle).then(price => {
    if (price) {
        console.log(JSON.stringify({ result: price }));
    } else {
        console.log(JSON.stringify({ isError: true, result: "No prices found" }));
    }
});
