/**
 * Script to crawl Vietnam map data from sapnhap.bando.com.vn
 * Downloads:
 * - 34 provinces GeoJSON
 * - Ward/commune GeoJSON for each province
 */

const fs = require('fs');
const https = require('https');
const path = require('path');

const BASE_URL = 'https://sapnhap.bando.com.vn';
const OUTPUT_DIR = path.join(__dirname, '../public/data/new-provinces');
const WARDS_DIR = path.join(OUTPUT_DIR, 'wards');

// Ensure directories exist
if (!fs.existsSync(OUTPUT_DIR)) {
    fs.mkdirSync(OUTPUT_DIR, { recursive: true });
}
if (!fs.existsSync(WARDS_DIR)) {
    fs.mkdirSync(WARDS_DIR, { recursive: true });
}

/**
 * Download file from URL
 */
function downloadFile(url, dest) {
    return new Promise((resolve, reject) => {
        console.log(`📥 Downloading: ${url}`);

        const file = fs.createWriteStream(dest);

        https.get(url, (response) => {
            if (response.statusCode !== 200) {
                reject(new Error(`Failed to download ${url}: ${response.statusCode}`));
                return;
            }

            response.pipe(file);

            file.on('finish', () => {
                file.close();
                console.log(`✅ Saved: ${dest}`);
                resolve();
            });
        }).on('error', (err) => {
            fs.unlink(dest, () => { });
            reject(err);
        });
    });
}

/**
 * Validate GeoJSON file
 */
function validateGeoJSON(filePath) {
    try {
        const content = fs.readFileSync(filePath, 'utf8');
        const data = JSON.parse(content);

        if (data.type !== 'FeatureCollection') {
            throw new Error('Invalid GeoJSON: not a FeatureCollection');
        }

        if (!Array.isArray(data.features)) {
            throw new Error('Invalid GeoJSON: features is not an array');
        }

        console.log(`✅ Valid GeoJSON: ${filePath} (${data.features.length} features)`);
        return true;
    } catch (error) {
        console.error(`❌ Invalid GeoJSON: ${filePath}`, error.message);
        return false;
    }
}

/**
 * Main crawl function
 */
async function crawlData() {
    console.log('🚀 Starting data crawl from sapnhap.bando.com.vn\n');

    try {
        // Step 1: Download provinces GeoJSON
        console.log('📍 Step 1: Downloading 34 provinces data...');
        const provincesUrl = `${BASE_URL}/vietnam_sapnhap_tinh.geojson`;
        const provincesDest = path.join(OUTPUT_DIR, 'provinces.geojson');

        await downloadFile(provincesUrl, provincesDest);
        validateGeoJSON(provincesDest);

        console.log('\n');

        // Step 2: Download wards GeoJSON for each province
        console.log('📍 Step 2: Downloading ward data for each province...');
        console.log('Province IDs: 1000-1033 (34 provinces)\n');

        let successCount = 0;
        let failCount = 0;

        for (let id = 1000; id <= 1033; id++) {
            const wardsUrl = `${BASE_URL}/vietnam_sapnhap_xa_${id}.geojson`;
            const wardsDest = path.join(WARDS_DIR, `province-${id}.geojson`);

            try {
                await downloadFile(wardsUrl, wardsDest);

                if (validateGeoJSON(wardsDest)) {
                    successCount++;
                } else {
                    failCount++;
                }

                // Add small delay to avoid overwhelming the server
                await new Promise(resolve => setTimeout(resolve, 500));
            } catch (error) {
                console.error(`❌ Failed to download province ${id}:`, error.message);
                failCount++;
            }
        }

        console.log('\n');
        console.log('═══════════════════════════════════════');
        console.log('✅ CRAWL COMPLETED!');
        console.log('═══════════════════════════════════════');
        console.log(`📊 Statistics:`);
        console.log(`   - Provinces: 1 file`);
        console.log(`   - Wards: ${successCount} files (${failCount} failed)`);
        console.log(`   - Total: ${successCount + 1} files`);
        console.log(`\n📁 Output directory: ${OUTPUT_DIR}`);

    } catch (error) {
        console.error('❌ Crawl failed:', error);
        process.exit(1);
    }
}

// Run the crawler
crawlData();
