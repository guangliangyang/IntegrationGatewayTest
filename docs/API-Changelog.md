# API Changelog

## Version 2.0.0

### New Features

- **`POST /api/v2/products/batch`** - Batch create products, supports creating multiple products in a single operation
- **`GET /api/v2/products/{id}/history`** - Get product history records, including version change information
- **ProductV2Dto Enhanced Model** - New fields added:
  - `supplier` - Supplier information
  - `tags` - Product tags array
  - `metadata` - Custom metadata dictionary

### Changes

- **`GET /api/v2/products`** - Returns enhanced product list with supplier, tags, and metadata fields
- **`GET /api/v2/products/{id}`** - Returns enhanced product details with additional V2 fields
- **`POST /api/v2/products`** - Returns enhanced V2 response format after product creation
- **`PUT /api/v2/products/{id}`** - Returns enhanced V2 response format after product update

### Deprecations

- None

### Removals

- None

---

**Backward Compatibility**: V2 is fully backward compatible with V1. All V1 functionality remains unchanged in V2, with only additional response fields and new endpoints added.