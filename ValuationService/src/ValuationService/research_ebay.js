const { chromium } = require('playwright');

async function getEbayAveragePrice(title) {
    const browser = await chromium.launch({ 
        headless: true,
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage']
    });
    
    // Use a realistic user agent to avoid being blocked
    const context = await browser.newContext({
        userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36',
        viewport: { width: 1280, height: 720 }
    });

    const page = await context.newPage();
    
    // Abort unnecessary requests to save memory and speed up
    await page.route('**/*.{png,jpg,jpeg,gif,webp,svg,css,woff,woff2}', route => route.abort());

    try {
        const searchQuery = encodeURIComponent(title + " comic sold");
        const url = `https://www.ebay.com/sch/i.html?_nkw=${searchQuery}&_sacat=0&LH_Sold=1&LH_Complete=1`;
        
        console.error(`Navigating to: ${url}`);
        
        const response = await page.goto(url, { 
            waitUntil: 'domcontentloaded', 
            timeout: 30000 
        });

        if (response.status() !== 200) {
            console.error(`Error: Received status ${response.status()}`);
            return null;
        }

        // Wait a bit for potential JS to render results if domcontentloaded wasn't enough
        await page.waitForTimeout(2000);

        const pageTitle = await page.title();
        if (pageTitle.includes('Pardon Our Interruption')) {
            console.error('Blocked by eBay bot detection');
            return null;
        }

        // Extract prices from the search results
        // eBay's structure for sold prices: span.s-item__price span.POSITIVE
        // We also try more generic selectors if the specific one fails
        const prices = await page.evaluate(() => {
            const selectors = [
                '.s-item__price span.POSITIVE',
                '.s-item__price',
                '.POSITIVE'
            ];
            
            for (const selector of selectors) {
                const elements = Array.from(document.querySelectorAll(selector));
                const found = elements.map(el => {
                    const text = el.innerText.replace(/[^\d.]/g, '');
                    return parseFloat(text);
                }).filter(p => !isNaN(p) && p > 0.1);
                
                if (found.length > 0) return found;
            }
            return [];
        });

        console.error(`Found ${prices.length} prices: ${JSON.stringify(prices.slice(0, 3))}`);

        if (prices.length === 0) {
            return null;
        }

        // Take up to 5 prices for a better average
        const sample = prices.slice(0, 5);
        const average = sample.reduce((a, b) => a + b, 0) / sample.length;
        return average.toFixed(2);

    } catch (error) {
        console.error("Error during research:", error);
        return null;
    } finally {
        await browser.close();
    }
}

const title = process.argv.slice(2).join(' ');
if (!title) {
    console.error("No title provided");
    process.exit(1);
}

getEbayAveragePrice(title).then(price => {
    if (price) {
        console.log(JSON.stringify({ result: price }));
    } else {
        console.log(JSON.stringify({ isError: true, result: "No prices found" }));
    }
});
