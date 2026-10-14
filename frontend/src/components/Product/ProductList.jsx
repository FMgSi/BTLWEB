import ProductCard from './ProductCard';

export default function ProductList({ products, onSelectProduct, onAddToCart }) {
  return (
    <div className="row row-cols-2 row-cols-sm-3 row-cols-md-3 row-cols-lg-4 row-cols-xl-5 g-2">
      {products.map((item) => (
        <div className="col" key={item.id}>
          <ProductCard item={item} onSelectProduct={onSelectProduct} onAddToCart={onAddToCart} />
        </div>
      ))}
    </div>
  );
}
